using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using Microsoft.AspNetCore.Authorization;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador")]
    public class LotesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LotesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // LISTADO
        // =========================
        public async Task<IActionResult> Index()
        {
            var lotes = _context.Lotes
                .Include(l => l.Productor)
                .OrderByDescending(l => l.FechaRecepcion);

            return View(await lotes.ToListAsync());
        }

        // =========================
        // DETALLES
        // =========================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lote = await _context.Lotes
                .Include(l => l.Productor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (lote == null)
            {
                return NotFound();
            }

            return View(lote);
        }

        // =========================
        // CREAR
        // =========================
        public IActionResult Create()
        {
            ViewData["ProductorId"] = new SelectList(
                _context.Productores.Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " - " + p.Cedula
                }),
                "Id",
                "NombreCompleto"
            );
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,CodigoLote,ProductorId,PesoKg,FechaRecepcion,Estado,Observacion")]
            Lote lote)
        {
            if (ModelState.IsValid)
            {
                _context.Add(lote);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductorId"] = new SelectList(
                _context.Productores.Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " - " + p.Cedula
                }),
                "Id",
                "NombreCompleto",
                lote.ProductorId
            );
            return View(lote);
        }
        // =========================
        // EDITAR
        // =========================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var lote = await _context.Lotes.FindAsync(id);
            if (lote == null)
            {
                return NotFound();
            }
            ViewData["ProductorId"] = new SelectList(
                _context.Productores.Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " - " + p.Cedula
                }),
                "Id",
                "NombreCompleto",
                lote.ProductorId
            );
            return View(lote);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,CodigoLote,ProductorId,PesoKg,FechaRecepcion,Estado,Observacion")]
            Lote lote)
        {
            if (id != lote.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lote);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoteExists(lote.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductorId"] = new SelectList(
                _context.Productores.Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " - " + p.Cedula
                }),
                "Id",
                "NombreCompleto",
                lote.ProductorId
            );
            return View(lote);
        }
        // =========================
        // ELIMINAR
        // =========================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lote = await _context.Lotes
                .Include(l => l.Productor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lote == null)
            {
                return NotFound();
            }

            return View(lote);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var lote = await _context.Lotes.FindAsync(id);

                if (lote == null)
                {
                    return NotFound();
                }

                _context.Lotes.Remove(lote);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Lote eliminado correctamente.";
            }
            catch
            {
                TempData["Error"] =
                    "No se puede eliminar el lote porque tiene información relacionada con producción.";
            }
            return RedirectToAction(nameof(Index));
        }
        // =========================
        // VALIDACIÓN
        // =========================
        private bool LoteExists(int id)
        {
            return _context.Lotes.Any(e => e.Id == id);
        }
    }
}