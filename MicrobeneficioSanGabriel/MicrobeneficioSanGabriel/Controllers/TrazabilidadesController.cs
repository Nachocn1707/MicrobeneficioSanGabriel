using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.ViewModels;
using MicrobeneficioSanGabriel.Services;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador,Cliente")]
    public class TrazabilidadesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrazabilidadesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? codigoLote, string? etapa)
        {
            if (User.IsInRole("Cliente"))
            {
                TempData["Info"] = "La trazabilidad interna no está disponible para el perfil cliente. Podés consultar el estado de tus pedidos desde Mis pedidos.";
                return RedirectToAction("ClienteDashboard", "Home");
            }

            // Autorreparación: si existen producciones completadas creadas antes de
            // esta versión o algún registro automático faltó, se reconstruye desde
            // la fuente real antes de presentar la trazabilidad.
            await TrazabilidadProcesoHelper.ReconciliarAsync(_context, User);

            var trazabilidadesQuery = await _context.Trazabilidades
                .Where(t => t.EsAutomatico)
                .Include(t => t.Lote)
                    .ThenInclude(l => l!.Productor)
                .Include(t => t.Lote)
                    .ThenInclude(l => l!.Finca)
                .Include(t => t.Produccion)
                    .ThenInclude(p => p!.Producto)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(codigoLote))
            {
                trazabilidadesQuery = trazabilidadesQuery
                    .Where(t => t.Lote != null && t.Lote.CodigoLote.Contains(codigoLote, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(etapa))
            {
                trazabilidadesQuery = trazabilidadesQuery
                    .Where(t => string.Equals(t.Etapa, etapa, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Garantiza que en la tabla principal de Trazabilidad solo se muestre 1 fila por lote (su etapa actual).
            var trazabilidadesUnicasPorLote = trazabilidadesQuery
                .GroupBy(t => t.LoteId)
                .Select(g => g
                    .OrderByDescending(t => Array.IndexOf(TrazabilidadProcesoHelper.EtapasOrdenadas, TrazabilidadProcesoHelper.EtapasOrdenadas.FirstOrDefault(e => string.Equals(e, t.Etapa, StringComparison.OrdinalIgnoreCase)) ?? ""))
                    .ThenByDescending(t => t.FechaRegistro)
                    .ThenByDescending(t => t.Id)
                    .First())
                .OrderByDescending(t => t.FechaRegistro)
                .ThenByDescending(t => t.Id)
                .ToList();

            ViewBag.CodigoLote = codigoLote;
            ViewBag.Etapa = etapa;

            ViewBag.Etapas = new SelectList(TrazabilidadProcesoHelper.EtapasOrdenadas, etapa);

            return View(trazabilidadesUnicasPorLote);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (User.IsInRole("Cliente"))
            {
                return RedirectToAction("ClienteDashboard", "Home");
            }
            if (id == null)
            {
                return NotFound();
            }

            var trazabilidad = await _context.Trazabilidades
                .Include(t => t.Lote)
                    .ThenInclude(l => l!.Productor)
                .Include(t => t.Lote)
                    .ThenInclude(l => l!.Finca)
                .Include(t => t.Produccion)
                    .ThenInclude(p => p!.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trazabilidad == null)
            {
                return NotFound();
            }

            return View(trazabilidad);
        }

        public async Task<IActionResult> Historial(int? loteId)
        {
            if (User.IsInRole("Cliente"))
            {
                return RedirectToAction("ClienteDashboard", "Home");
            }
            if (loteId == null)
            {
                return NotFound();
            }

            await TrazabilidadProcesoHelper.ReconciliarAsync(_context, User);

            var lote = await _context.Lotes
                .Include(l => l.Productor)
                .Include(l => l.Finca)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == loteId);

            if (lote == null)
            {
                return NotFound();
            }

            var producciones = await _context.Producciones
                .Include(p => p.Producto)
                .Where(p => p.LoteId == lote.Id)
                .OrderBy(p => p.FechaProduccion)
                .ToListAsync();

            var trazabilidades = await _context.Trazabilidades
                .Include(t => t.Produccion)
                    .ThenInclude(p => p!.Producto)
                .Where(t => t.LoteId == lote.Id && t.EsAutomatico)
                .OrderBy(t => t.FechaRegistro)
                .ToListAsync();

            var model = new TrazabilidadHistorialViewModel
            {
                Lote = lote,
                Productor = lote.Productor,
                Producciones = producciones,
                Trazabilidades = trazabilidades
            };

            return View(model);
        }

        [Authorize(Roles = "Administrador,Operador")]
        public IActionResult Create()
        {
            TempData["Info"] = "La trazabilidad se genera automáticamente desde las producciones completadas. No es necesario crear etapas manualmente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public Task<IActionResult> Create([Bind("LoteId,ProduccionId,Etapa,FechaRegistro,Responsable,Observacion")] Trazabilidad trazabilidad)
        {
            TempData["Info"] = "La trazabilidad es automática y se genera al completar cada proceso de producción.";
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));
        }

        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var trazabilidad = await _context.Trazabilidades.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (trazabilidad == null) return NotFound();

            if (trazabilidad.ProduccionId > 0)
            {
                TempData["Info"] = "Redirigido al módulo de producción para gestionar la etapa o estado del proceso.";
                return RedirectToAction("Edit", "Producciones", new { id = trazabilidad.ProduccionId });
            }

            TempData["Info"] = "Los registros históricos manuales se conservan únicamente como referencia.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public Task<IActionResult> Edit(int id, [Bind("Id,LoteId,ProduccionId,Etapa,FechaRegistro,Responsable,Observacion")] Trazabilidad trazabilidad)
        {
            if (trazabilidad.ProduccionId > 0)
            {
                TempData["Info"] = "Redirigido al módulo de producción para gestionar la etapa o estado del proceso.";
                return Task.FromResult<IActionResult>(RedirectToAction("Edit", "Producciones", new { id = trazabilidad.ProduccionId }));
            }

            TempData["Info"] = "La trazabilidad se modifica desde el flujo real de producción.";
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(Details), new { id }));
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trazabilidad = await _context.Trazabilidades
                .Include(t => t.Lote)
                .Include(t => t.Produccion)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trazabilidad == null)
            {
                return NotFound();
            }

            if (trazabilidad.EsAutomatico)
            {
                TempData["Info"] = "Los registros automáticos no se eliminan desde trazabilidad. Se sincronizan con la producción relacionada.";
                return RedirectToAction(nameof(Details), new { id = trazabilidad.Id });
            }

            return View(trazabilidad);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trazabilidad = await _context.Trazabilidades.FindAsync(id);

            if (trazabilidad != null)
            {
                if (trazabilidad.EsAutomatico)
                {
                    TempData["Error"] = "No puede eliminar una etapa automática. Corrija la producción relacionada.";
                    return RedirectToAction(nameof(Details), new { id = trazabilidad.Id });
                }

                _context.Trazabilidades.Remove(trazabilidad);
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Trazabilidad", "Eliminar", id,
                    $"Se eliminó el registro de trazabilidad #{id}.");
                TempData["Success"] = "Registro de trazabilidad eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EsOperadorSoloEtapa()
        {
            return User.IsInRole("Operador") && !User.IsInRole("Administrador");
        }

        private static bool EtapaValida(string? etapa)
        {
            return etapa is "Lavado" or "Secado" or "Tostado" or "Molido" or "Empaque";
        }

        private async Task ValidarRelacionAsync(Trazabilidad trazabilidad)
        {
            var produccion = await _context.Producciones
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == trazabilidad.ProduccionId);

            if (produccion == null)
            {
                ModelState.AddModelError(nameof(Trazabilidad.ProduccionId),
                    "La producción seleccionada no existe.");
                return;
            }

            if (produccion.LoteId != trazabilidad.LoteId)
            {
                ModelState.AddModelError(nameof(Trazabilidad.ProduccionId),
                    "La producción seleccionada no pertenece al lote indicado.");
            }
        }

        private void CargarListas(int? loteId = null, int? produccionId = null)
        {
            ViewData["LoteId"] = new SelectList(
                _context.Lotes
                    .Include(l => l.Finca)
                    .Include(l => l.Productor)
                    .OrderBy(l => l.CodigoLote)
                    .AsEnumerable()
                    .Select(l => new
                    {
                        l.Id,
                        Descripcion = l.CodigoLote + " - " +
                            (l.Finca != null && !string.IsNullOrWhiteSpace(l.Finca.Nombre)
                                ? l.Finca.Nombre
                                : l.Productor != null && !string.IsNullOrWhiteSpace(l.Productor.Nombre)
                                    ? l.Productor.Nombre
                                    : "Sin nombre")
                    })
                    .ToList(),
                "Id",
                "Descripcion",
                loteId
            );

            ViewData["ProduccionId"] = new SelectList(
                _context.Producciones
                    .Include(p => p.Lote)
                    .OrderByDescending(p => p.FechaProduccion)
                    .Select(p => new
                    {
                        p.Id,
                        Descripcion = $"#{p.Id} - {p.Lote!.CodigoLote} - {p.TipoProceso} - {p.Estado}"
                    }),
                "Id",
                "Descripcion",
                produccionId
            );

            ViewBag.Etapas = new SelectList(TrazabilidadProcesoHelper.EtapasOrdenadas);
        }

        private bool TrazabilidadExists(int id)
        {
            return _context.Trazabilidades.Any(e => e.Id == id);
        }
    }
}