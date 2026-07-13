using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador")]
    public class ProductoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productores = await _context.Productores
                .Include(p => p.Fincas)
                .Include(p => p.Lotes)
                .OrderByDescending(p => p.FechaRegistro)
                .ToListAsync();

            foreach (var productor in productores)
            {
                productor.Telefono = SoloDigitos(productor.Telefono);
            }

            return View(productores);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var productor = await _context.Productores
                .Include(p => p.Fincas)
                .Include(p => p.Lotes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (productor == null) return NotFound();
            productor.Telefono = SoloDigitos(productor.Telefono);
            return View(productor);
        }

        public IActionResult Create()
        {
            return View(new Productor
            {
                Activo = true,
                FechaRegistro = DateTime.Now
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre,Cedula,Telefono,Correo,Provincia,Canton,Distrito,DireccionExacta,Activo")]
            Productor productor)
        {
            Normalizar(productor);
            await ValidarDuplicadosAsync(productor);

            if (ModelState.IsValid)
            {
                try
                {
                    productor.FechaRegistro = DateTime.Now;
                    productor.Direccion = ConstruirDireccion(productor);

                    _context.Productores.Add(productor);
                    await _context.SaveChangesAsync();
                    await AuditoriaHelper.RegistrarAsync(
                        _context, User, "Productores", "Crear", productor.Id,
                        $"Se registró al productor {productor.Nombre}.");

                    TempData["Success"] = "Productor registrado correctamente. Ahora puede asociarle una o más fincas.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty,
                        "No fue posible registrar el productor. Verifique que la cédula y el correo no estén duplicados.");
                }
            }

            return View(productor);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var productor = await _context.Productores.FindAsync(id);
            if (productor == null) return NotFound();
            productor.Telefono = SoloDigitos(productor.Telefono);
            return View(productor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Nombre,Cedula,Telefono,Correo,Provincia,Canton,Distrito,DireccionExacta,Activo")]
            Productor productor)
        {
            if (id != productor.Id) return NotFound();

            var original = await _context.Productores
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (original == null) return NotFound();

            Normalizar(productor);
            await ValidarDuplicadosAsync(productor, id);

            if (ModelState.IsValid)
            {
                try
                {
                    productor.FechaRegistro = original.FechaRegistro;
                    productor.Finca = original.Finca;
                    productor.Direccion = ConstruirDireccion(productor);

                    _context.Update(productor);
                    await _context.SaveChangesAsync();
                    await AuditoriaHelper.RegistrarAsync(
                        _context, User, "Productores", "Editar", productor.Id,
                        $"Se actualizó al productor {productor.Nombre}.");

                    TempData["Success"] = "Productor actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductorExists(productor.Id)) return NotFound();
                    throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty,
                        "No fue posible actualizar el productor. Verifique la información ingresada.");
                }
            }

            return View(productor);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var productor = await _context.Productores
                .Include(p => p.Fincas)
                .Include(p => p.Lotes)
                .FirstOrDefaultAsync(p => p.Id == id);

            return productor == null ? NotFound() : View(productor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productor = await _context.Productores
                .Include(p => p.Fincas)
                .Include(p => p.Lotes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (productor == null) return NotFound();

            var cantidadLotes = productor.Lotes.Count;
            if (cantidadLotes > 0)
            {
                TempData["Error"] =
                    $"No se puede eliminar el productor porque posee {cantidadLotes} lote(s) asociado(s). Elimine o reasigne esos lotes primero.";
                return RedirectToAction(nameof(Index));
            }

            var cantidadFincas = productor.Fincas.Count;
            if (cantidadFincas > 0)
            {
                TempData["Error"] =
                    $"No se puede eliminar el productor porque posee {cantidadFincas} finca(s) registrada(s). Elimine las fincas primero o desactive el productor.";
                return RedirectToAction(nameof(Index));
            }

            _context.Productores.Remove(productor);
            await _context.SaveChangesAsync();
            await AuditoriaHelper.RegistrarAsync(
                _context, User, "Productores", "Eliminar", id,
                $"Se eliminó al productor {productor.Nombre}.");

            TempData["Success"] = "Productor eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidarDuplicadosAsync(Productor productor, int? excluirId = null)
        {
            var cedulaDuplicada = await _context.Productores.AnyAsync(p =>
                p.Cedula == productor.Cedula && (!excluirId.HasValue || p.Id != excluirId));

            if (cedulaDuplicada)
            {
                ModelState.AddModelError(nameof(Productor.Cedula),
                    "Ya existe un productor registrado con esta cédula.");
            }

            if (!string.IsNullOrWhiteSpace(productor.Correo))
            {
                var correoNormalizado = productor.Correo.ToLower();
                var correoDuplicado = await _context.Productores.AnyAsync(p =>
                    p.Correo != null && p.Correo.ToLower() == correoNormalizado &&
                    (!excluirId.HasValue || p.Id != excluirId));

                if (correoDuplicado)
                {
                    ModelState.AddModelError(nameof(Productor.Correo),
                        "Ya existe un productor registrado con este correo electrónico.");
                }
            }
        }

        private static string SoloDigitos(string? valor)
        {
            return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        }

        private static void Normalizar(Productor productor)
        {
            productor.Nombre = productor.Nombre?.Trim() ?? string.Empty;
            productor.Cedula = new string((productor.Cedula ?? string.Empty).Where(char.IsDigit).ToArray());
            productor.Telefono = SoloDigitos(productor.Telefono);
            productor.Correo = string.IsNullOrWhiteSpace(productor.Correo)
                ? null
                : productor.Correo.Trim().ToLowerInvariant();
            productor.Provincia = productor.Provincia?.Trim() ?? string.Empty;
            productor.Canton = productor.Canton?.Trim() ?? string.Empty;
            productor.Distrito = productor.Distrito?.Trim() ?? string.Empty;
            productor.DireccionExacta = productor.DireccionExacta?.Trim() ?? string.Empty;
        }

        private static string ConstruirDireccion(Productor productor)
        {
            return $"{productor.Provincia}, {productor.Canton}, {productor.Distrito}. {productor.DireccionExacta}";
        }

        private bool ProductorExists(int id) => _context.Productores.Any(e => e.Id == id);
    }
}
