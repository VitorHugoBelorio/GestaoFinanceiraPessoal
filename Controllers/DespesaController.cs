using GestaoFinanceiraPessoal.Data;
using GestaoFinanceiraPessoal.Extensions;
using GestaoFinanceiraPessoal.Models;
using GestaoFinanceiraPessoal.Models.ViewModels;
using GestaoFinanceiraPessoal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Controllers
{
    [Authorize]
    [Route("despesas")]
    public class DespesaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RecorrenciaService _recorrenciaService;

        public DespesaController(AppDbContext context, RecorrenciaService recorrenciaService)
        {
            _context = context;
            _recorrenciaService = recorrenciaService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int mes, int ano, bool exibirCanceladas = false)
        {
            if (mes == 0) mes = DateTime.Now.Month;
            if (ano == 0) ano = DateTime.Now.Year;

            var inicioMes = new DateTime(ano, mes, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);
            var userId = User.GetRequiredUserId();

            await _recorrenciaService.MaterializarCompetenciasAteAsync(userId, ano, mes);

            var despesas = await _context.Despesas
                .Include(d => d.Categoria)
                .Where(d => d.UserId == userId
                    && !d.EhModeloRecorrente
                    && (exibirCanceladas || !d.OcorrenciaCancelada)
                    && d.Data >= inicioMes
                    && d.Data <= fimMes)
                .OrderBy(d => d.OcorrenciaCancelada)
                .ThenBy(d => d.Data)
                .ThenBy(d => d.Descricao)
                .ToListAsync();

            var model = new DespesaIndexViewModel
            {
                Ano = ano,
                Mes = mes,
                Total = despesas.Where(d => !d.OcorrenciaCancelada).Sum(d => d.Valor),
                ExibirCanceladas = exibirCanceladas,
                Itens = despesas
                    .Select(d => new DespesaListaItemViewModel
                    {
                        Id = d.Id,
                        Descricao = d.Descricao,
                        CategoriaNome = d.Categoria?.Nome ?? "Sem categoria",
                        Status = d.Status,
                        Valor = d.Valor,
                        DataExibicao = d.Data,
                        Recorrente = d.RecorrenciaId != null,
                        OcorrenciaCancelada = d.OcorrenciaCancelada
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpGet("novo")]
        public async Task<IActionResult> Create()
        {
            var userId = User.GetRequiredUserId();
            await PopularCategoriasAsync(userId);
            return View(new DespesaFormViewModel());
        }

        [HttpPost("novo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DespesaFormViewModel model)
        {
            var userId = User.GetRequiredUserId();

            if (!ModelState.IsValid)
            {
                await PopularCategoriasAsync(userId);
                return View(model);
            }

            if (model.Recorrente)
            {
                var recorrencia = _recorrenciaService.CriarRecorrencia(
                    model.Data,
                    model.RecorrenciaTipo,
                    model.RecorrenciaIntervalo,
                    model.RecorrenciaDataFim);

                var modelo = new Despesa
                {
                    Descricao = model.Descricao,
                    Valor = model.Valor,
                    Data = model.Data.Date,
                    CategoriaId = model.CategoriaId,
                    Status = StatusDespesa.Normalizar(model.Status),
                    EhModeloRecorrente = true,
                    DataReferenciaRecorrencia = model.Data.Date,
                    Recorrencia = recorrencia,
                    UserId = userId
                };

                var primeiraCompetencia = _recorrenciaService.CriarOcorrenciaDespesa(modelo, model.Data.Date);
                _context.Despesas.AddRange(modelo, primeiraCompetencia);
            }
            else
            {
                _context.Despesas.Add(new Despesa
                {
                    Descricao = model.Descricao,
                    Valor = model.Valor,
                    Data = model.Data.Date,
                    CategoriaId = model.CategoriaId,
                    Status = StatusDespesa.Normalizar(model.Status),
                    UserId = userId
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente && !d.OcorrenciaCancelada);

            if (despesa == null)
            {
                return NotFound();
            }

            await PopularCategoriasAsync(userId);
            return View(MapearParaFormulario(despesa));
        }

        [HttpPost("editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DespesaFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente && !d.OcorrenciaCancelada);

            if (despesa == null)
            {
                return NotFound();
            }

            model.PermiteEditarConfiguracaoRecorrencia = despesa.RecorrenciaId == null;
            model.ExibirAvisoEdicaoCompetencia = despesa.RecorrenciaId != null;

            if (!ModelState.IsValid)
            {
                await PopularCategoriasAsync(userId);
                return View(model);
            }

            despesa.Descricao = model.Descricao;
            despesa.Valor = model.Valor;
            despesa.Data = model.Data.Date;
            despesa.CategoriaId = model.CategoriaId;
            despesa.Status = StatusDespesa.Normalizar(model.Status);

            if (despesa.RecorrenciaId == null && model.Recorrente)
            {
                var recorrencia = _recorrenciaService.CriarRecorrencia(
                    model.Data,
                    model.RecorrenciaTipo,
                    model.RecorrenciaIntervalo,
                    model.RecorrenciaDataFim);

                despesa.Recorrencia = recorrencia;
                despesa.DataReferenciaRecorrencia = model.Data.Date;

                var modelo = new Despesa
                {
                    Descricao = model.Descricao,
                    Valor = model.Valor,
                    Data = model.Data.Date,
                    CategoriaId = model.CategoriaId,
                    Status = StatusDespesa.Normalizar(model.Status),
                    EhModeloRecorrente = true,
                    DataReferenciaRecorrencia = model.Data.Date,
                    Recorrencia = recorrencia,
                    UserId = userId
                };

                _context.Despesas.Add(modelo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar-serie/{id:int}")]
        public async Task<IActionResult> EditSeries(int id)
        {
            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente && !d.OcorrenciaCancelada && d.RecorrenciaId != null);

            if (despesa == null)
            {
                return NotFound();
            }

            await PopularCategoriasAsync(userId);
            ConfigurarModoEdicaoSerie(despesa, false);
            return View("Edit", MapearParaFormulario(despesa));
        }

        [HttpPost("editar-serie/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSeries(int id, DespesaFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente && !d.OcorrenciaCancelada && d.RecorrenciaId != null);

            if (despesa == null || !despesa.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            var modeloSerie = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.UserId == userId && d.EhModeloRecorrente && d.RecorrenciaId == despesa.RecorrenciaId);

            if (modeloSerie == null)
            {
                return NotFound();
            }

            if (model.Data.Year != despesa.Data.Year || model.Data.Month != despesa.Data.Month)
            {
                ModelState.AddModelError(nameof(model.Data), "A serie deve continuar dentro do mesmo mes da competencia selecionada.");
            }

            if (!ModelState.IsValid)
            {
                await PopularCategoriasAsync(userId);
                ConfigurarModoEdicaoSerie(despesa, false);
                return View("Edit", model);
            }

            var dataReferenciaOriginal = (despesa.DataReferenciaRecorrencia ?? despesa.Data).Date;

            despesa.Descricao = model.Descricao;
            despesa.Valor = model.Valor;
            despesa.Data = model.Data.Date;
            despesa.DataReferenciaRecorrencia = model.Data.Date;
            despesa.CategoriaId = model.CategoriaId;
            despesa.Status = StatusDespesa.Normalizar(model.Status);
            despesa.OcorrenciaCancelada = false;

            modeloSerie.Descricao = model.Descricao;
            modeloSerie.Valor = model.Valor;
            modeloSerie.Data = model.Data.Date;
            modeloSerie.DataReferenciaRecorrencia = model.Data.Date;
            modeloSerie.CategoriaId = model.CategoriaId;
            modeloSerie.Status = StatusDespesa.Normalizar(model.Status);
            modeloSerie.OcorrenciaCancelada = false;

            if (modeloSerie.Recorrencia == null)
            {
                modeloSerie.Recorrencia = _recorrenciaService.CriarRecorrencia(
                    model.Data,
                    model.RecorrenciaTipo,
                    model.RecorrenciaIntervalo,
                    model.RecorrenciaDataFim);
            }
            else
            {
                _recorrenciaService.ReconfigurarRecorrencia(
                    modeloSerie.Recorrencia,
                    model.Data,
                    model.RecorrenciaTipo,
                    model.RecorrenciaIntervalo,
                    model.RecorrenciaDataFim);
            }

            await _recorrenciaService.RemoverOcorrenciasDespesaAPartirDeAsync(
                userId,
                despesa.RecorrenciaId.Value,
                dataReferenciaOriginal,
                despesa.Id);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { ano = model.Data.Year, mes = model.Data.Month });
        }

        [HttpGet("editar-proximas/{id:int}")]
        public async Task<IActionResult> EditFutureSeries(int id)
        {
            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente && !d.OcorrenciaCancelada && d.RecorrenciaId != null);

            if (despesa == null || despesa.Recorrencia == null || !despesa.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            var modeloSerie = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.UserId == userId && d.EhModeloRecorrente && d.RecorrenciaId == despesa.RecorrenciaId);

            if (modeloSerie == null)
            {
                return NotFound();
            }

            var dataReferenciaAtual = (despesa.DataReferenciaRecorrencia ?? despesa.Data).Date;
            var proximaCompetencia = _recorrenciaService.ObterProximaDataRecorrencia(despesa.Recorrencia, dataReferenciaAtual);
            var model = MapearParaFormulario(modeloSerie);

            model.Id = despesa.Id;
            model.Data = proximaCompetencia;

            await PopularCategoriasAsync(userId);
            ConfigurarModoEdicaoSerie(despesa, true, proximaCompetencia);
            return View("Edit", model);
        }

        [HttpPost("editar-proximas/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFutureSeries(int id, DespesaFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente && !d.OcorrenciaCancelada && d.RecorrenciaId != null);

            if (despesa == null || despesa.Recorrencia == null || !despesa.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            var modeloSerie = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.UserId == userId && d.EhModeloRecorrente && d.RecorrenciaId == despesa.RecorrenciaId);

            if (modeloSerie == null)
            {
                return NotFound();
            }

            var dataReferenciaAtual = (despesa.DataReferenciaRecorrencia ?? despesa.Data).Date;
            var proximaCompetenciaOriginal = _recorrenciaService.ObterProximaDataRecorrencia(despesa.Recorrencia, dataReferenciaAtual);

            if (model.Data.Date < proximaCompetenciaOriginal.Date)
            {
                ModelState.AddModelError(nameof(model.Data), "A nova competencia base deve ser igual ou posterior a proxima competencia prevista da serie.");
            }

            if (!ModelState.IsValid)
            {
                await PopularCategoriasAsync(userId);
                ConfigurarModoEdicaoSerie(despesa, true, model.Data.Date);
                return View("Edit", model);
            }

            modeloSerie.Descricao = model.Descricao;
            modeloSerie.Valor = model.Valor;
            modeloSerie.Data = model.Data.Date;
            modeloSerie.DataReferenciaRecorrencia = model.Data.Date;
            modeloSerie.CategoriaId = model.CategoriaId;
            modeloSerie.Status = StatusDespesa.Normalizar(model.Status);
            modeloSerie.OcorrenciaCancelada = false;

            if (modeloSerie.Recorrencia == null)
            {
                modeloSerie.Recorrencia = _recorrenciaService.CriarRecorrencia(
                    model.Data,
                    model.RecorrenciaTipo,
                    model.RecorrenciaIntervalo,
                    model.RecorrenciaDataFim);
            }
            else
            {
                _recorrenciaService.ReconfigurarRecorrencia(
                    modeloSerie.Recorrencia,
                    model.Data,
                    model.RecorrenciaTipo,
                    model.RecorrenciaIntervalo,
                    model.RecorrenciaDataFim);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _recorrenciaService.RemoverOcorrenciasDespesaAPartirDeAsync(
                userId,
                despesa.RecorrenciaId.Value,
                proximaCompetenciaOriginal);

            await _context.SaveChangesAsync();

            _context.Despesas.Add(_recorrenciaService.CriarOcorrenciaDespesa(modeloSerie, model.Data.Date));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Index), new { ano = model.Data.Year, mes = model.Data.Month });
        }

        [HttpGet("{id:int}/excluir")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente);

            if (despesa == null)
            {
                return NotFound();
            }

            if (despesa.RecorrenciaId != null)
            {
                despesa.OcorrenciaCancelada = true;
            }
            else
            {
                _context.Despesas.Remove(despesa);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { ano = despesa.Data.Year, mes = despesa.Data.Month });
        }

        [HttpPost("encerrar-serie/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndSeries(int id)
        {
            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente && !d.OcorrenciaCancelada && d.RecorrenciaId != null);

            if (despesa == null || despesa.Recorrencia == null || !despesa.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            _recorrenciaService.EncerrarRecorrenciaAPartirDe(despesa.Recorrencia, despesa.Data);

            await _recorrenciaService.CancelarOcorrenciasDespesaAPartirDeAsync(
                userId,
                despesa.RecorrenciaId.Value,
                despesa.Data);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { ano = despesa.Data.Year, mes = despesa.Data.Month });
        }

        [HttpPost("encerrar-serie-apos/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndSeriesAfterCurrent(int id)
        {
            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .Include(d => d.Recorrencia)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente && !d.OcorrenciaCancelada && d.RecorrenciaId != null);

            if (despesa == null || despesa.Recorrencia == null || !despesa.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            var dataReferenciaAtual = (despesa.DataReferenciaRecorrencia ?? despesa.Data).Date;
            var proximaCompetencia = _recorrenciaService.ObterProximaDataRecorrencia(despesa.Recorrencia, dataReferenciaAtual);

            _recorrenciaService.EncerrarRecorrenciaApos(despesa.Recorrencia, dataReferenciaAtual);

            await _recorrenciaService.CancelarOcorrenciasDespesaAPartirDeAsync(
                userId,
                despesa.RecorrenciaId.Value,
                proximaCompetencia);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { ano = despesa.Data.Year, mes = despesa.Data.Month });
        }

        [HttpPost("restaurar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = User.GetRequiredUserId();

            var despesa = await _context.Despesas
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId && !d.EhModeloRecorrente && d.RecorrenciaId != null && d.OcorrenciaCancelada);

            if (despesa == null)
            {
                return NotFound();
            }

            despesa.OcorrenciaCancelada = false;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { ano = despesa.Data.Year, mes = despesa.Data.Month, exibirCanceladas = true });
        }

        private void ConfigurarModoEdicaoSerie(Despesa despesa, bool somenteProximas, DateTime? competenciaBase = null)
        {
            ViewBag.EditandoSerie = true;
            ViewBag.EditandoSomenteProximas = somenteProximas;
            ViewBag.CompetenciaOrigem = despesa.Data;
            ViewBag.CompetenciaBase = competenciaBase ?? despesa.Data;
        }

        private async Task PopularCategoriasAsync(int userId)
        {
            ViewBag.Categorias = await _context.Categorias
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Nome)
                .ToListAsync();
        }

        private static DespesaFormViewModel MapearParaFormulario(Despesa despesa)
        {
            return new DespesaFormViewModel
            {
                Id = despesa.Id,
                Descricao = despesa.Descricao,
                Valor = despesa.Valor,
                Data = despesa.Data,
                CategoriaId = despesa.CategoriaId,
                Status = despesa.Status,
                Recorrente = despesa.RecorrenciaId != null,
                RecorrenciaTipo = despesa.Recorrencia?.Tipo ?? RecorrenciaTipos.Mensal,
                RecorrenciaIntervalo = despesa.Recorrencia?.Intervalo ?? 1,
                RecorrenciaDataFim = despesa.Recorrencia?.DataFim,
                PermiteEditarConfiguracaoRecorrencia = despesa.RecorrenciaId == null,
                ExibirAvisoEdicaoCompetencia = despesa.RecorrenciaId != null
            };
        }
    }
}
