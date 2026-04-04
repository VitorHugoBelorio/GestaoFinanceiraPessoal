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
    [Route("receitas")]
    public class ReceitaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RecorrenciaService _recorrenciaService;

        public ReceitaController(AppDbContext context, RecorrenciaService recorrenciaService)
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

            var receitas = await _context.Receitas
                .Where(r => r.UserId == userId
                    && !r.EhModeloRecorrente
                    && (exibirCanceladas || !r.OcorrenciaCancelada)
                    && r.Data >= inicioMes
                    && r.Data <= fimMes)
                .OrderBy(r => r.OcorrenciaCancelada)
                .ThenBy(r => r.Data)
                .ThenBy(r => r.Descricao)
                .ToListAsync();

            var model = new ReceitaIndexViewModel
            {
                Ano = ano,
                Mes = mes,
                Total = receitas.Where(r => !r.OcorrenciaCancelada).Sum(r => r.Valor),
                ExibirCanceladas = exibirCanceladas,
                Itens = receitas
                    .Select(r => new ReceitaListaItemViewModel
                    {
                        Id = r.Id,
                        Descricao = r.Descricao,
                        Valor = r.Valor,
                        DataExibicao = r.Data,
                        Recorrente = r.RecorrenciaId != null,
                        OcorrenciaCancelada = r.OcorrenciaCancelada
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpGet("novo")]
        public IActionResult Create()
        {
            return View(new ReceitaFormViewModel());
        }

        [HttpPost("novo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReceitaFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.GetRequiredUserId();

            if (model.Recorrente)
            {
                var recorrencia = _recorrenciaService.CriarRecorrencia(
                    model.Data,
                    model.RecorrenciaTipo,
                    model.RecorrenciaIntervalo,
                    model.RecorrenciaDataFim);

                var modelo = new Receita
                {
                    Descricao = model.Descricao,
                    Valor = model.Valor,
                    Data = model.Data.Date,
                    EhModeloRecorrente = true,
                    DataReferenciaRecorrencia = model.Data.Date,
                    Recorrencia = recorrencia,
                    UserId = userId
                };

                var primeiraCompetencia = _recorrenciaService.CriarOcorrenciaReceita(modelo, model.Data.Date);
                _context.Receitas.AddRange(modelo, primeiraCompetencia);
            }
            else
            {
                _context.Receitas.Add(new Receita
                {
                    Descricao = model.Descricao,
                    Valor = model.Valor,
                    Data = model.Data.Date,
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

            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente && !r.OcorrenciaCancelada);

            if (receita == null)
            {
                return NotFound();
            }

            return View(MapearParaFormulario(receita));
        }

        [HttpPost("editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReceitaFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var userId = User.GetRequiredUserId();

            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente && !r.OcorrenciaCancelada);

            if (receita == null)
            {
                return NotFound();
            }

            model.PermiteEditarConfiguracaoRecorrencia = receita.RecorrenciaId == null;
            model.ExibirAvisoEdicaoCompetencia = receita.RecorrenciaId != null;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            receita.Descricao = model.Descricao;
            receita.Valor = model.Valor;
            receita.Data = model.Data.Date;

            if (receita.RecorrenciaId == null && model.Recorrente)
            {
                var recorrencia = _recorrenciaService.CriarRecorrencia(
                    model.Data,
                    model.RecorrenciaTipo,
                    model.RecorrenciaIntervalo,
                    model.RecorrenciaDataFim);

                receita.Recorrencia = recorrencia;
                receita.DataReferenciaRecorrencia = model.Data.Date;

                var modelo = new Receita
                {
                    Descricao = model.Descricao,
                    Valor = model.Valor,
                    Data = model.Data.Date,
                    EhModeloRecorrente = true,
                    DataReferenciaRecorrencia = model.Data.Date,
                    Recorrencia = recorrencia,
                    UserId = userId
                };

                _context.Receitas.Add(modelo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar-serie/{id:int}")]
        public async Task<IActionResult> EditSeries(int id)
        {
            var userId = User.GetRequiredUserId();

            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente && !r.OcorrenciaCancelada && r.RecorrenciaId != null);

            if (receita == null)
            {
                return NotFound();
            }

            var model = MapearParaFormulario(receita);
            ConfigurarModoEdicaoSerie(receita, false);
            return View("Edit", model);
        }

        [HttpPost("editar-serie/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSeries(int id, ReceitaFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var userId = User.GetRequiredUserId();

            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente && !r.OcorrenciaCancelada && r.RecorrenciaId != null);

            if (receita == null || !receita.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            var modeloSerie = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.EhModeloRecorrente && r.RecorrenciaId == receita.RecorrenciaId);

            if (modeloSerie == null)
            {
                return NotFound();
            }

            if (model.Data.Year != receita.Data.Year || model.Data.Month != receita.Data.Month)
            {
                ModelState.AddModelError(nameof(model.Data), "A serie deve continuar dentro do mesmo mes da competencia selecionada.");
            }

            if (!ModelState.IsValid)
            {
                ConfigurarModoEdicaoSerie(receita, false);
                return View("Edit", model);
            }

            var dataReferenciaOriginal = (receita.DataReferenciaRecorrencia ?? receita.Data).Date;

            receita.Descricao = model.Descricao;
            receita.Valor = model.Valor;
            receita.Data = model.Data.Date;
            receita.DataReferenciaRecorrencia = model.Data.Date;
            receita.OcorrenciaCancelada = false;

            modeloSerie.Descricao = model.Descricao;
            modeloSerie.Valor = model.Valor;
            modeloSerie.Data = model.Data.Date;
            modeloSerie.DataReferenciaRecorrencia = model.Data.Date;
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

            await _recorrenciaService.RemoverOcorrenciasReceitaAPartirDeAsync(
                userId,
                receita.RecorrenciaId.Value,
                dataReferenciaOriginal,
                receita.Id);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { ano = model.Data.Year, mes = model.Data.Month });
        }

        [HttpGet("editar-proximas/{id:int}")]
        public async Task<IActionResult> EditFutureSeries(int id)
        {
            var userId = User.GetRequiredUserId();

            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente && !r.OcorrenciaCancelada && r.RecorrenciaId != null);

            if (receita == null || receita.Recorrencia == null || !receita.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            var modeloSerie = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.EhModeloRecorrente && r.RecorrenciaId == receita.RecorrenciaId);

            if (modeloSerie == null)
            {
                return NotFound();
            }

            var dataReferenciaAtual = (receita.DataReferenciaRecorrencia ?? receita.Data).Date;
            var proximaCompetencia = _recorrenciaService.ObterProximaDataRecorrencia(receita.Recorrencia, dataReferenciaAtual);
            var model = MapearParaFormulario(modeloSerie);

            model.Id = receita.Id;
            model.Data = proximaCompetencia;

            ConfigurarModoEdicaoSerie(receita, true, proximaCompetencia);
            return View("Edit", model);
        }

        [HttpPost("editar-proximas/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFutureSeries(int id, ReceitaFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var userId = User.GetRequiredUserId();

            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente && !r.OcorrenciaCancelada && r.RecorrenciaId != null);

            if (receita == null || receita.Recorrencia == null || !receita.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            var modeloSerie = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.EhModeloRecorrente && r.RecorrenciaId == receita.RecorrenciaId);

            if (modeloSerie == null)
            {
                return NotFound();
            }

            var dataReferenciaAtual = (receita.DataReferenciaRecorrencia ?? receita.Data).Date;
            var proximaCompetenciaOriginal = _recorrenciaService.ObterProximaDataRecorrencia(receita.Recorrencia, dataReferenciaAtual);

            if (model.Data.Date < proximaCompetenciaOriginal.Date)
            {
                ModelState.AddModelError(nameof(model.Data), "A nova competencia base deve ser igual ou posterior a proxima competencia prevista da serie.");
            }

            if (!ModelState.IsValid)
            {
                ConfigurarModoEdicaoSerie(receita, true, model.Data.Date);
                return View("Edit", model);
            }

            modeloSerie.Descricao = model.Descricao;
            modeloSerie.Valor = model.Valor;
            modeloSerie.Data = model.Data.Date;
            modeloSerie.DataReferenciaRecorrencia = model.Data.Date;
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

            await _recorrenciaService.RemoverOcorrenciasReceitaAPartirDeAsync(
                userId,
                receita.RecorrenciaId.Value,
                proximaCompetenciaOriginal);

            await _context.SaveChangesAsync();

            _context.Receitas.Add(_recorrenciaService.CriarOcorrenciaReceita(modeloSerie, model.Data.Date));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Index), new { ano = model.Data.Year, mes = model.Data.Month });
        }

        [HttpGet("excluir/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetRequiredUserId();

            var receita = await _context.Receitas
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente);

            if (receita == null)
            {
                return NotFound();
            }

            if (receita.RecorrenciaId != null)
            {
                receita.OcorrenciaCancelada = true;
            }
            else
            {
                _context.Receitas.Remove(receita);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { ano = receita.Data.Year, mes = receita.Data.Month });
        }

        [HttpPost("encerrar-serie/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndSeries(int id)
        {
            var userId = User.GetRequiredUserId();

            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente && !r.OcorrenciaCancelada && r.RecorrenciaId != null);

            if (receita == null || receita.Recorrencia == null || !receita.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            _recorrenciaService.EncerrarRecorrenciaAPartirDe(receita.Recorrencia, receita.Data);

            await _recorrenciaService.CancelarOcorrenciasReceitaAPartirDeAsync(
                userId,
                receita.RecorrenciaId.Value,
                receita.Data);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { ano = receita.Data.Year, mes = receita.Data.Month });
        }

        [HttpPost("encerrar-serie-apos/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndSeriesAfterCurrent(int id)
        {
            var userId = User.GetRequiredUserId();

            var receita = await _context.Receitas
                .Include(r => r.Recorrencia)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente && !r.OcorrenciaCancelada && r.RecorrenciaId != null);

            if (receita == null || receita.Recorrencia == null || !receita.RecorrenciaId.HasValue)
            {
                return NotFound();
            }

            var dataReferenciaAtual = (receita.DataReferenciaRecorrencia ?? receita.Data).Date;
            var proximaCompetencia = _recorrenciaService.ObterProximaDataRecorrencia(receita.Recorrencia, dataReferenciaAtual);

            _recorrenciaService.EncerrarRecorrenciaApos(receita.Recorrencia, dataReferenciaAtual);

            await _recorrenciaService.CancelarOcorrenciasReceitaAPartirDeAsync(
                userId,
                receita.RecorrenciaId.Value,
                proximaCompetencia);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { ano = receita.Data.Year, mes = receita.Data.Month });
        }

        [HttpPost("restaurar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = User.GetRequiredUserId();

            var receita = await _context.Receitas
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.EhModeloRecorrente && r.RecorrenciaId != null && r.OcorrenciaCancelada);

            if (receita == null)
            {
                return NotFound();
            }

            receita.OcorrenciaCancelada = false;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { ano = receita.Data.Year, mes = receita.Data.Month, exibirCanceladas = true });
        }

        private void ConfigurarModoEdicaoSerie(Receita receita, bool somenteProximas, DateTime? competenciaBase = null)
        {
            ViewBag.EditandoSerie = true;
            ViewBag.EditandoSomenteProximas = somenteProximas;
            ViewBag.CompetenciaOrigem = receita.Data;
            ViewBag.CompetenciaBase = competenciaBase ?? receita.Data;
        }

        private static ReceitaFormViewModel MapearParaFormulario(Receita receita)
        {
            return new ReceitaFormViewModel
            {
                Id = receita.Id,
                Descricao = receita.Descricao,
                Valor = receita.Valor,
                Data = receita.Data,
                Recorrente = receita.RecorrenciaId != null,
                RecorrenciaTipo = receita.Recorrencia?.Tipo ?? RecorrenciaTipos.Mensal,
                RecorrenciaIntervalo = receita.Recorrencia?.Intervalo ?? 1,
                RecorrenciaDataFim = receita.Recorrencia?.DataFim,
                PermiteEditarConfiguracaoRecorrencia = receita.RecorrenciaId == null,
                ExibirAvisoEdicaoCompetencia = receita.RecorrenciaId != null
            };
        }
    }
}
