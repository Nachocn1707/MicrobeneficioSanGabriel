using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using Microsoft.AspNetCore.Authorization;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize]
    public class ProduccionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProduccionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTA
        public async Task<IActionResult> Index()
        {
            var producciones = _context.Producciones
                .Include(p => p.Lote);

            return View(await producciones.ToListAsync());
        }

        // DETALLES
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var produccion = await _context.Producciones
                .Include(p => p.Lote)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (produccion == null)
                return NotFound();

            return View(produccion);
        }

        // CREAR
        public IActionResult Create()
        {
            ViewBag.LoteId = new SelectList(
                _context.Lotes,
                "Id",
                "CodigoLote"
            );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Produccion produccion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(produccion);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.LoteId = new SelectList(
                _context.Lotes,
                "Id",
                "CodigoLote",
                produccion.LoteId
            );

            return View(produccion);
        }

        // EDITAR
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var produccion = await _context.Producciones.FindAsync(id);

            if (produccion == null)
                return NotFound();

            ViewBag.LoteId = new SelectList(
                _context.Lotes,
                "Id",
                "CodigoLote",
                produccion.LoteId
            );

            return View(produccion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Produccion produccion)
        {
            if (id != produccion.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(produccion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Producciones.Any(e => e.Id == produccion.Id))
                        return NotFound();

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.LoteId = new SelectList(
                _context.Lotes,
                "Id",
                "CodigoLote",
                produccion.LoteId
            );

            return View(produccion);
        }

        // ELIMINAR
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var produccion = await _context.Producciones
                .Include(p => p.Lote)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (produccion == null)
                return NotFound();

            return View(produccion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var produccion = await _context.Producciones.FindAsync(id);

            if (produccion != null)
            {
                _context.Producciones.Remove(produccion);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}