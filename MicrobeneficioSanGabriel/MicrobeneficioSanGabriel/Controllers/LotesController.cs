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
    public class LotesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LotesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var lotes = await _context.Lotes
                .Include(l => l.Productor)
                .Include(l => l.Finca)
                .OrderByDescending(l => l.FechaRecepcion)
                .ToListAsync();

            return View(lotes);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var lote = await _context.Lotes
                .Include(l => l.Productor)
                .Include(l => l.Finca)
                .FirstOrDefaultAsync(l => l.Id == id);

            return lote == null ? NotFound() : View(lote);
        }

        public IActionResult Create()
        {
            CargarFincas();

            return View(new Lote
            {
                FechaRecepcion = DateTime.Now,
                Estado = "Recibido"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("FincaId,PesoKg,FechaRecepcion,Estado,Observacion")]
            Lote lote)
        {
            var finca = lote.FincaId.HasValue
                ? await _context.Fincas
                    .Include(f => f.Productor)
                    .FirstOrDefaultAsync(f => f.Id == lote.FincaId.Value && f.Activa)
                : null;

            if (finca == null)
            {
                ModelState.AddModelError(nameof(Lote.FincaId), "Seleccione una finca activa y válida.");
            }
            else
            {
                lote.ProductorId = finca.ProductorId;
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Código temporal corto para que no falle el límite de 30 caracteres.
                    // Antes estaba usando TEMP + Guid completo y eso podía pasar el límite de la columna CodigoLote.
                    lote.CodigoLote = $"TMP-{Guid.NewGuid():N}".Substring(0, 30);

                    lote.Observacion = string.IsNullOrWhiteSpace(lote.Observacion)
                        ? null
                        : lote.Observacion.Trim();

                    _context.Lotes.Add(lote);
                    await _context.SaveChangesAsync();

                    // Código final visible para el usuario.
                    lote.CodigoLote = $"SG-{lote.FechaRecepcion:yyyy}-{lote.Id:D5}";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await AuditoriaHelper.RegistrarAsync(
                        _context,
                        User,
                        "Lotes",
                        "Crear",
                        lote.Id,
                        $"Se registró el lote {lote.CodigoLote} para la finca {finca!.Nombre}.");

                    TempData["Success"] = $"Lote {lote.CodigoLote} registrado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    await transaction.RollbackAsync();

                    ModelState.AddModelError(
                        string.Empty,
                        "No fue posible registrar el lote. Revise la información e inténtelo nuevamente.");
                }
            }

            CargarFincas(lote.FincaId);
            return View(lote);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var lote = await _context.Lotes.FindAsync(id);

            if (lote == null) return NotFound();

            CargarFincas(lote.FincaId);
            return View(lote);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,FincaId,PesoKg,FechaRecepcion,Estado,Observacion")]
            Lote lote)
        {
            if (id != lote.Id) return NotFound();

            var original = await _context.Lotes
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (original == null) return NotFound();

            var finca = lote.FincaId.HasValue
                ? await _context.Fincas.FirstOrDefaultAsync(f => f.Id == lote.FincaId.Value)
                : null;

            if (finca == null)
            {
                ModelState.AddModelError(nameof(Lote.FincaId), "Seleccione una finca válida.");
            }
            else
            {
                lote.ProductorId = finca.ProductorId;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    lote.CodigoLote = original.CodigoLote;

                    lote.Observacion = string.IsNullOrWhiteSpace(lote.Observacion)
                        ? null
                        : lote.Observacion.Trim();

                    _context.Update(lote);
                    await _context.SaveChangesAsync();

                    await AuditoriaHelper.RegistrarAsync(
                        _context,
                        User,
                        "Lotes",
                        "Editar",
                        lote.Id,
                        $"Se actualizó el lote {lote.CodigoLote}.");

                    TempData["Success"] = $"Lote {lote.CodigoLote} actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoteExists(lote.Id)) return NotFound();
                    throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No fue posible actualizar el lote. Verifique sus relaciones.");
                }
            }

            CargarFincas(lote.FincaId);
            return View(lote);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var lote = await _context.Lotes
                .Include(l => l.Productor)
                .Include(l => l.Finca)
                .FirstOrDefaultAsync(l => l.Id == id);

            return lote == null ? NotFound() : View(lote);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var lote = await _context.Lotes.FindAsync(id);

                if (lote == null) return NotFound();

                var producciones = await _context.Producciones
                    .Include(p => p.Producto)
                    .Where(p => p.LoteId == id)
                    .ToListAsync();

                var cantidadesPorProducto = producciones
                    .Where(p => p.Estado == "Completado" && p.ProductoId.HasValue)
                    .GroupBy(p => p.ProductoId!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(p => (int)p.CantidadResultanteKg));

                foreach (var item in cantidadesPorProducto)
                {
                    var producto = producciones.First(p => p.ProductoId == item.Key).Producto;

                    if (producto == null || producto.Stock < item.Value)
                    {
                        TempData["Error"] =
                            "No se puede eliminar el lote porque parte del inventario generado por sus producciones ya fue utilizado.";

                        return RedirectToAction(nameof(Index));
                    }
                }

                foreach (var item in cantidadesPorProducto)
                {
                    var producto = producciones.First(p => p.ProductoId == item.Key).Producto!;
                    producto.Stock -= item.Value;
                }

                var idsProducciones = producciones.Select(p => p.Id).ToList();

                var trazabilidades = await _context.Trazabilidades
                    .Where(t => t.LoteId == id || idsProducciones.Contains(t.ProduccionId))
                    .ToListAsync();

                var referencias = producciones
                    .Select(p => $"Entrada automática por producción #{p.Id}")
                    .ToList();

                var movimientos = referencias.Count == 0
                    ? new List<MovimientoInventario>()
                    : await _context.MovimientosInventario
                        .Where(m => m.TipoMovimiento == "Entrada" &&
                                    m.Observacion != null &&
                                    referencias.Contains(m.Observacion))
                        .ToListAsync();

                var codigo = lote.CodigoLote;

                _context.Trazabilidades.RemoveRange(trazabilidades);
                _context.MovimientosInventario.RemoveRange(movimientos);
                _context.Producciones.RemoveRange(producciones);
                _context.Lotes.Remove(lote);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await AuditoriaHelper.RegistrarAsync(
                    _context,
                    User,
                    "Lotes",
                    "Eliminar",
                    id,
                    $"Se eliminó el lote {codigo} y sus registros dependientes.");

                TempData["Success"] = $"Lote {codigo} eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "No fue posible eliminar el lote porque todavía tiene información relacionada.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void CargarFincas(int? seleccionada = null)
        {
            var fincas = _context.Fincas
                .Where(f => f.Activa && f.Productor != null && f.Productor.Activo)
                .OrderBy(f => f.Productor!.Nombre)
                .ThenBy(f => f.Nombre)
                .Select(f => new
                {
                    f.Id,
                    Texto = f.Productor!.Nombre + " — " + f.Nombre + " (" + f.Distrito + ", " + f.Canton + ")"
                });

            ViewBag.FincaId = new SelectList(fincas, "Id", "Texto", seleccionada);
        }

        private bool LoteExists(int id)
        {
            return _context.Lotes.Any(e => e.Id == id);
        }
    }
}