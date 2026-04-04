using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Services
{
    public class RecorrenciaService
    {
        private readonly AppDbContext _context;

        public RecorrenciaService(AppDbContext context)
        {
            _context = context;
        }

        public Recorrencia CriarRecorrencia(DateTime dataInicial)
        {
            return CriarRecorrencia(dataInicial, RecorrenciaTipos.Mensal, 1, null);
        }

        public Recorrencia CriarRecorrencia(DateTime dataInicial, string tipo, int intervalo, DateTime? dataFim)
        {
            var dataBase = dataInicial.Date;
            var tipoNormalizado = RecorrenciaTipos.Normalizar(tipo);
            var intervaloNormalizado = Math.Max(1, intervalo);

            var recorrencia = new Recorrencia
            {
                Tipo = tipoNormalizado,
                Intervalo = intervaloNormalizado,
                DiaReferencia = dataBase.Day,
                MesReferencia = dataBase.Month,
                DataFim = dataFim?.Date
            };

            recorrencia.ProximaExecucao = CalcularProximaExecucao(recorrencia, dataBase);
            return recorrencia;
        }

        public Receita CriarOcorrenciaReceita(Receita modelo, DateTime dataReferencia)
        {
            return new Receita
            {
                Descricao = modelo.Descricao,
                Valor = modelo.Valor,
                Data = dataReferencia.Date,
                DataReferenciaRecorrencia = dataReferencia.Date,
                EhModeloRecorrente = false,
                OcorrenciaCancelada = false,
                Recorrencia = modelo.Recorrencia,
                RecorrenciaId = modelo.RecorrenciaId,
                UserId = modelo.UserId
            };
        }

        public Despesa CriarOcorrenciaDespesa(Despesa modelo, DateTime dataReferencia)
        {
            return new Despesa
            {
                Descricao = modelo.Descricao,
                Valor = modelo.Valor,
                Data = dataReferencia.Date,
                DataReferenciaRecorrencia = dataReferencia.Date,
                EhModeloRecorrente = false,
                OcorrenciaCancelada = false,
                CategoriaId = modelo.CategoriaId,
                Status = modelo.Status,
                Recorrencia = modelo.Recorrencia,
                RecorrenciaId = modelo.RecorrenciaId,
                UserId = modelo.UserId
            };
        }

        public void Desativar(Recorrencia recorrencia)
        {
            recorrencia.DataFim = DateTime.Today;
        }

        public void ReconfigurarRecorrencia(Recorrencia recorrencia, DateTime dataInicial, string tipo, int intervalo, DateTime? dataFim)
        {
            var dataBase = dataInicial.Date;

            recorrencia.Tipo = RecorrenciaTipos.Normalizar(tipo);
            recorrencia.Intervalo = Math.Max(1, intervalo);
            recorrencia.DiaReferencia = dataBase.Day;
            recorrencia.MesReferencia = dataBase.Month;
            recorrencia.DataFim = dataFim?.Date;
            recorrencia.ProximaExecucao = CalcularProximaExecucao(recorrencia, dataBase);
        }

        public DateTime ObterProximaDataRecorrencia(Recorrencia recorrencia, DateTime dataReferencia)
        {
            return CalcularProximaExecucao(recorrencia, dataReferencia.Date);
        }

        public void EncerrarRecorrenciaAPartirDe(Recorrencia recorrencia, DateTime dataReferencia)
        {
            recorrencia.DataFim = dataReferencia.Date.AddDays(-1);
        }

        public void EncerrarRecorrenciaApos(Recorrencia recorrencia, DateTime dataReferencia)
        {
            recorrencia.DataFim = dataReferencia.Date;
        }

        public async Task RemoverOcorrenciasReceitaAPartirDeAsync(int userId, int recorrenciaId, DateTime dataReferencia, int? preservarId = null)
        {
            var ocorrencias = await _context.Receitas
                .Where(r => r.UserId == userId
                    && !r.EhModeloRecorrente
                    && r.RecorrenciaId == recorrenciaId
                    && r.DataReferenciaRecorrencia != null
                    && r.DataReferenciaRecorrencia >= dataReferencia.Date
                    && (!preservarId.HasValue || r.Id != preservarId.Value))
                .ToListAsync();

            if (ocorrencias.Count > 0)
            {
                _context.Receitas.RemoveRange(ocorrencias);
            }
        }

        public async Task RemoverOcorrenciasDespesaAPartirDeAsync(int userId, int recorrenciaId, DateTime dataReferencia, int? preservarId = null)
        {
            var ocorrencias = await _context.Despesas
                .Where(d => d.UserId == userId
                    && !d.EhModeloRecorrente
                    && d.RecorrenciaId == recorrenciaId
                    && d.DataReferenciaRecorrencia != null
                    && d.DataReferenciaRecorrencia >= dataReferencia.Date
                    && (!preservarId.HasValue || d.Id != preservarId.Value))
                .ToListAsync();

            if (ocorrencias.Count > 0)
            {
                _context.Despesas.RemoveRange(ocorrencias);
            }
        }

        public async Task CancelarOcorrenciasReceitaAPartirDeAsync(int userId, int recorrenciaId, DateTime dataReferencia)
        {
            var ocorrencias = await _context.Receitas
                .Where(r => r.UserId == userId
                    && !r.EhModeloRecorrente
                    && r.RecorrenciaId == recorrenciaId
                    && r.DataReferenciaRecorrencia != null
                    && r.DataReferenciaRecorrencia >= dataReferencia.Date)
                .ToListAsync();

            foreach (var ocorrencia in ocorrencias)
            {
                ocorrencia.OcorrenciaCancelada = true;
            }
        }

        public async Task CancelarOcorrenciasDespesaAPartirDeAsync(int userId, int recorrenciaId, DateTime dataReferencia)
        {
            var ocorrencias = await _context.Despesas
                .Where(d => d.UserId == userId
                    && !d.EhModeloRecorrente
                    && d.RecorrenciaId == recorrenciaId
                    && d.DataReferenciaRecorrencia != null
                    && d.DataReferenciaRecorrencia >= dataReferencia.Date)
                .ToListAsync();

            foreach (var ocorrencia in ocorrencias)
            {
                ocorrencia.OcorrenciaCancelada = true;
            }
        }

        public async Task MaterializarCompetenciasAteAsync(int userId, int ano, int mes)
        {
            var limite = new DateTime(ano, mes, 1).AddMonths(1).AddDays(-1);

            await MaterializarReceitasAsync(userId, limite);
            await MaterializarDespesasAsync(userId, limite);

            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task MaterializarReceitasAsync(int userId, DateTime limiteInclusive)
        {
            var modelos = await _context.Receitas
                .Include(r => r.Recorrencia)
                .Where(r => r.UserId == userId && r.EhModeloRecorrente && r.RecorrenciaId != null && r.Recorrencia != null)
                .ToListAsync();

            if (modelos.Count == 0)
            {
                return;
            }

            var recorrenciaIds = modelos
                .Where(r => r.RecorrenciaId.HasValue)
                .Select(r => r.RecorrenciaId!.Value)
                .ToList();

            var chavesExistentes = await _context.Receitas
                .Where(r => r.UserId == userId && !r.EhModeloRecorrente && r.RecorrenciaId != null && r.DataReferenciaRecorrencia != null && recorrenciaIds.Contains(r.RecorrenciaId.Value))
                .Select(r => CriarChaveOcorrencia(r.RecorrenciaId!.Value, r.DataReferenciaRecorrencia!.Value))
                .ToListAsync();

            var ocorrenciasExistentes = new HashSet<string>(chavesExistentes);

            foreach (var modelo in modelos)
            {
                var recorrencia = modelo.Recorrencia;
                if (recorrencia == null || !modelo.RecorrenciaId.HasValue)
                {
                    continue;
                }

                var dataBase = (modelo.DataReferenciaRecorrencia ?? modelo.Data).Date;
                var proximaExecucao = ObterProximaExecucaoInicial(recorrencia, dataBase);

                while (DeveGerarOcorrencia(proximaExecucao, recorrencia.DataFim, limiteInclusive))
                {
                    var chave = CriarChaveOcorrencia(modelo.RecorrenciaId.Value, proximaExecucao);

                    if (!ocorrenciasExistentes.Contains(chave))
                    {
                        _context.Receitas.Add(CriarOcorrenciaReceita(modelo, proximaExecucao));
                        ocorrenciasExistentes.Add(chave);
                    }

                    proximaExecucao = CalcularProximaExecucao(recorrencia, proximaExecucao);
                }

                recorrencia.ProximaExecucao = proximaExecucao;
            }
        }

        private async Task MaterializarDespesasAsync(int userId, DateTime limiteInclusive)
        {
            var modelos = await _context.Despesas
                .Include(d => d.Recorrencia)
                .Where(d => d.UserId == userId && d.EhModeloRecorrente && d.RecorrenciaId != null && d.Recorrencia != null)
                .ToListAsync();

            if (modelos.Count == 0)
            {
                return;
            }

            var recorrenciaIds = modelos
                .Where(d => d.RecorrenciaId.HasValue)
                .Select(d => d.RecorrenciaId!.Value)
                .ToList();

            var chavesExistentes = await _context.Despesas
                .Where(d => d.UserId == userId && !d.EhModeloRecorrente && d.RecorrenciaId != null && d.DataReferenciaRecorrencia != null && recorrenciaIds.Contains(d.RecorrenciaId.Value))
                .Select(d => CriarChaveOcorrencia(d.RecorrenciaId!.Value, d.DataReferenciaRecorrencia!.Value))
                .ToListAsync();

            var ocorrenciasExistentes = new HashSet<string>(chavesExistentes);

            foreach (var modelo in modelos)
            {
                var recorrencia = modelo.Recorrencia;
                if (recorrencia == null || !modelo.RecorrenciaId.HasValue)
                {
                    continue;
                }

                var dataBase = (modelo.DataReferenciaRecorrencia ?? modelo.Data).Date;
                var proximaExecucao = ObterProximaExecucaoInicial(recorrencia, dataBase);

                while (DeveGerarOcorrencia(proximaExecucao, recorrencia.DataFim, limiteInclusive))
                {
                    var chave = CriarChaveOcorrencia(modelo.RecorrenciaId.Value, proximaExecucao);

                    if (!ocorrenciasExistentes.Contains(chave))
                    {
                        _context.Despesas.Add(CriarOcorrenciaDespesa(modelo, proximaExecucao));
                        ocorrenciasExistentes.Add(chave);
                    }

                    proximaExecucao = CalcularProximaExecucao(recorrencia, proximaExecucao);
                }

                recorrencia.ProximaExecucao = proximaExecucao;
            }
        }

        private static bool DeveGerarOcorrencia(DateTime dataReferencia, DateTime? dataFim, DateTime limiteInclusive)
        {
            return dataReferencia.Date <= limiteInclusive.Date
                && (!dataFim.HasValue || dataReferencia.Date <= dataFim.Value.Date);
        }

        private DateTime ObterProximaExecucaoInicial(Recorrencia recorrencia, DateTime dataBase)
        {
            var proximaExecucao = recorrencia.ProximaExecucao.Date;

            if (proximaExecucao <= dataBase.Date)
            {
                return CalcularProximaExecucao(recorrencia, dataBase.Date);
            }

            return proximaExecucao;
        }

        private DateTime CalcularProximaExecucao(Recorrencia recorrencia, DateTime dataReferencia)
        {
            var baseDate = dataReferencia.Date;

            return recorrencia.Tipo switch
            {
                var tipo when tipo == RecorrenciaTipos.Semanal => baseDate.AddDays(7 * recorrencia.Intervalo),
                var tipo when tipo == RecorrenciaTipos.Anual => CriarDataAnual(recorrencia, baseDate.AddYears(recorrencia.Intervalo).Year),
                _ => CriarDataMensal(recorrencia, baseDate.AddMonths(recorrencia.Intervalo))
            };
        }

        private static DateTime CriarDataMensal(Recorrencia recorrencia, DateTime dataBase)
        {
            var dia = Math.Min(recorrencia.DiaReferencia, DateTime.DaysInMonth(dataBase.Year, dataBase.Month));
            return new DateTime(dataBase.Year, dataBase.Month, dia);
        }

        private static DateTime CriarDataAnual(Recorrencia recorrencia, int ano)
        {
            var dia = Math.Min(recorrencia.DiaReferencia, DateTime.DaysInMonth(ano, recorrencia.MesReferencia));
            return new DateTime(ano, recorrencia.MesReferencia, dia);
        }

        private static string CriarChaveOcorrencia(int recorrenciaId, DateTime dataReferencia)
        {
            return $"{recorrenciaId}:{dataReferencia:yyyyMMdd}";
        }
    }
}
