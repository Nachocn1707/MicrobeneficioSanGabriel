using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador")]
    public class FincasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FincasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var fincas = await _context.Fincas
                .Include(f => f.Productor)
                .Include(f => f.Lotes)
                .OrderBy(f => f.Nombre)
                .ToListAsync();

            return View(fincas);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var finca = await _context.Fincas
                .Include(f => f.Productor)
                .Include(f => f.Lotes)
                .FirstOrDefaultAsync(f => f.Id == id);

            return finca == null ? NotFound() : View(finca);
        }

        public IActionResult Create(int? productorId = null)
        {
            CargarProductores(productorId);
            return View(new Finca { ProductorId = productorId ?? 0, Activa = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre,ProductorId,Provincia,Canton,Distrito,DireccionExacta,Activa")]
            Finca finca)
        {
            Normalizar(finca);
            await ValidarDuplicadoAsync(finca);

            if (ModelState.IsValid)
            {
                finca.FechaRegistro = DateTime.Now;
                _context.Fincas.Add(finca);
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Fincas", "Crear", finca.Id,
                    $"Se registró la finca {finca.Nombre}.");

                TempData["Success"] = "Finca registrada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            CargarProductores(finca.ProductorId);
            return View(finca);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var finca = await _context.Fincas.FindAsync(id);
            if (finca == null) return NotFound();

            CargarProductores(finca.ProductorId);
            return View(finca);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Nombre,ProductorId,Provincia,Canton,Distrito,DireccionExacta,Activa")]
            Finca finca)
        {
            if (id != finca.Id) return NotFound();

            var original = await _context.Fincas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (original == null) return NotFound();

            Normalizar(finca);
            await ValidarDuplicadoAsync(finca, id);

            if (ModelState.IsValid)
            {
                finca.FechaRegistro = original.FechaRegistro;
                _context.Update(finca);
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Fincas", "Editar", finca.Id,
                    $"Se actualizó la finca {finca.Nombre}.");

                TempData["Success"] = "Finca actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            CargarProductores(finca.ProductorId);
            return View(finca);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var finca = await _context.Fincas
                .Include(f => f.Productor)
                .Include(f => f.Lotes)
                .FirstOrDefaultAsync(f => f.Id == id);

            return finca == null ? NotFound() : View(finca);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var finca = await _context.Fincas
                .Include(f => f.Lotes)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (finca == null) return NotFound();

            if (finca.Lotes.Count > 0)
            {
                TempData["Error"] =
                    $"No se puede eliminar la finca porque tiene {finca.Lotes.Count} lote(s) asociado(s). Reasigne o elimine los lotes primero.";
                return RedirectToAction(nameof(Index));
            }

            _context.Fincas.Remove(finca);
            await _context.SaveChangesAsync();
            await AuditoriaHelper.RegistrarAsync(
                _context, User, "Fincas", "Eliminar", id,
                $"Se eliminó la finca {finca.Nombre}.");

            TempData["Success"] = "Finca eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private void CargarProductores(int? seleccionado = null)
        {
            var productores = _context.Productores
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    p.Id,
                    Texto = p.Nombre + " - " + p.Cedula
                });

            ViewBag.ProductorId = new SelectList(productores, "Id", "Texto", seleccionado);
        }

        private async Task ValidarDuplicadoAsync(Finca finca, int? excluirId = null)
        {
            var duplicada = await _context.Fincas.AnyAsync(f =>
                f.ProductorId == finca.ProductorId &&
                f.Nombre.ToLower() == finca.Nombre.ToLower() &&
                (!excluirId.HasValue || f.Id != excluirId));

            if (duplicada)
            {
                ModelState.AddModelError(nameof(Finca.Nombre),
                    "Este productor ya tiene una finca registrada con el mismo nombre.");
            }
        }

        private static void Normalizar(Finca finca)
        {
            finca.Nombre = finca.Nombre?.Trim() ?? string.Empty;
            finca.Provincia = finca.Provincia?.Trim() ?? string.Empty;
            finca.Canton = finca.Canton?.Trim() ?? string.Empty;
            finca.Distrito = finca.Distrito?.Trim() ?? string.Empty;
            finca.DireccionExacta = finca.DireccionExacta?.Trim() ?? string.Empty;
        }
    }
}
