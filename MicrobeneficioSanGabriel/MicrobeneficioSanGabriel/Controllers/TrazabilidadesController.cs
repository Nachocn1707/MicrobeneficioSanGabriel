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
            var trazabilidades = _context.Trazabilidades
                .Include(t => t.Lote)
                    .ThenInclude(l => l.Productor)
                .Include(t => t.Produccion)
                    .ThenInclude(p => p.Producto)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(codigoLote))
            {
                trazabilidades = trazabilidades
                    .Where(t => t.Lote != null && t.Lote.CodigoLote.Contains(codigoLote));
            }

            if (!string.IsNullOrWhiteSpace(etapa))
            {
                trazabilidades = trazabilidades
                    .Where(t => t.Etapa == etapa);
            }

            ViewBag.CodigoLote = codigoLote;
            ViewBag.Etapa = etapa;

            ViewBag.Etapas = new SelectList(new List<string>
            {
                "Recepción",
                "Producción",
                "Secado",
                "Empaque",
                "Almacenamiento"
            }, etapa);

            var resultado = await trazabilidades
                .OrderByDescending(t => t.FechaRegistro)
                .ToListAsync();

            return View(resultado);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trazabilidad = await _context.Trazabilidades
                .Include(t => t.Lote)
                    .ThenInclude(l => l.Productor)
                .Include(t => t.Produccion)
                    .ThenInclude(p => p.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trazabilidad == null)
            {
                return NotFound();
            }

            return View(trazabilidad);
        }

        public async Task<IActionResult> Historial(int? loteId)
        {
            if (loteId == null)
            {
                return NotFound();
            }

            var lote = await _context.Lotes
                .Include(l => l.Productor)
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
                    .ThenInclude(p => p.Producto)
                .Where(t => t.LoteId == lote.Id)
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
            CargarListas();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Create([Bind("LoteId,ProduccionId,Etapa,FechaRegistro,Responsable,Observacion")] Trazabilidad trazabilidad)
        {
            await ValidarRelacionAsync(trazabilidad);

            if (ModelState.IsValid)
            {
                trazabilidad.FechaRegistro = trazabilidad.FechaRegistro == default
                    ? DateTime.Now
                    : trazabilidad.FechaRegistro;
                trazabilidad.Responsable = trazabilidad.Responsable?.Trim();
                trazabilidad.Observacion = trazabilidad.Observacion?.Trim();

                _context.Add(trazabilidad);
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Trazabilidad", "Crear", trazabilidad.Id,
                    $"Se registró la etapa {trazabilidad.Etapa} para el lote #{trazabilidad.LoteId}.");

                TempData["Success"] = "Registro de trazabilidad creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            CargarListas(trazabilidad.LoteId, trazabilidad.ProduccionId);
            return View(trazabilidad);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trazabilidad = await _context.Trazabilidades.FindAsync(id);

            if (trazabilidad == null)
            {
                return NotFound();
            }

            CargarListas(trazabilidad.LoteId, trazabilidad.ProduccionId);
            return View(trazabilidad);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LoteId,ProduccionId,Etapa,FechaRegistro,Responsable,Observacion")] Trazabilidad trazabilidad)
        {
            if (id != trazabilidad.Id)
            {
                return NotFound();
            }

            await ValidarRelacionAsync(trazabilidad);

            if (ModelState.IsValid)
            {
                try
                {
                    trazabilidad.Responsable = trazabilidad.Responsable?.Trim();
                    trazabilidad.Observacion = trazabilidad.Observacion?.Trim();
                    _context.Update(trazabilidad);
                    await _context.SaveChangesAsync();
                    await AuditoriaHelper.RegistrarAsync(
                        _context, User, "Trazabilidad", "Editar", trazabilidad.Id,
                        $"Se actualizó el registro de trazabilidad #{trazabilidad.Id}.");
                    TempData["Success"] = "Registro de trazabilidad actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrazabilidadExists(trazabilidad.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            CargarListas(trazabilidad.LoteId, trazabilidad.ProduccionId);
            return View(trazabilidad);
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
                _context.Trazabilidades.Remove(trazabilidad);
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Trazabilidad", "Eliminar", id,
                    $"Se eliminó el registro de trazabilidad #{id}.");
                TempData["Success"] = "Registro de trazabilidad eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
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
                _context.Lotes.OrderBy(l => l.CodigoLote),
                "Id",
                "CodigoLote",
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

            ViewBag.Etapas = new SelectList(new List<string>
            {
                "Recepción",
                "Producción",
                "Secado",
                "Empaque",
                "Almacenamiento"
            });
        }

        private bool TrazabilidadExists(int id)
        {
            return _context.Trazabilidades.Any(e => e.Id == id);
        }
    }
}