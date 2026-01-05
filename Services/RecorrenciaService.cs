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

        // Criação padrão (mensal)
        public Recorrencia CriarRecorrencia(DateTime dataInicial)
        {
            return new Recorrencia
            {
                Tipo = "Mensal",
                Intervalo = 1,
                ProximaExecucao = dataInicial,
                DataFim = null
            };
        }

        // "Desativar" = encerrar recorrência
        public void Desativar(Recorrencia recorrencia)
        {
            recorrencia.DataFim = DateTime.Today;
        }

        // Processamento automático (futuro: job/cron)
        public async Task ProcessarRecorrenciasAsync()
        {
            var hoje = DateTime.Today;

            var recorrencias = await _context.Recorrencias
                .Where(r =>
                    r.ProximaExecucao <= hoje &&
                    (r.DataFim == null || r.ProximaExecucao <= r.DataFim))
                .ToListAsync();

            foreach (var recorrencia in recorrencias)
            {
                // Aqui futuramente:
                // - gerar Receita ou Despesa
                AtualizarProximaExecucao(recorrencia);
            }

            await _context.SaveChangesAsync();
        }

        private void AtualizarProximaExecucao(Recorrencia recorrencia)
        {
            recorrencia.ProximaExecucao = recorrencia.Tipo switch
            {
                "Mensal" => recorrencia.ProximaExecucao.AddMonths(recorrencia.Intervalo),
                "Anual" => recorrencia.ProximaExecucao.AddYears(recorrencia.Intervalo),
                "Semanal" => recorrencia.ProximaExecucao.AddDays(7 * recorrencia.Intervalo),
                _ => recorrencia.ProximaExecucao
            };
        }
    }
}
