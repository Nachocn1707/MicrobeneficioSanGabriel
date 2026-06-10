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
    public class ProduccionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProduccionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Producciones
        public async Task<IActionResult> Index()
        {
            var producciones = _context.Producciones
                .Include(p => p.Lote)
                .Include(p => p.Producto)
                .OrderByDescending(p => p.FechaProduccion);

            return View(await producciones.ToListAsync());
        }

        // GET: Producciones/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produccion = await _context.Producciones
                .Include(p => p.Lote)
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (produccion == null)
            {
                return NotFound();
            }

            return View(produccion);
        }

        // GET: Producciones/Create
        public IActionResult Create()
        {
            CargarCombos();
            return View();
        }

        // POST: Producciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Produccion produccion)
        {
            if (produccion.CantidadResultanteKg > produccion.CantidadProcesadaKg)
            {
                ModelState.AddModelError(nameof(Produccion.CantidadResultanteKg),
                    "La cantidad resultante no puede superar la cantidad procesada.");
            }

            if (ModelState.IsValid)
            {
                produccion.FechaProduccion = DateTime.Now;

                _context.Producciones.Add(produccion);
                await _context.SaveChangesAsync();

                // Solo aumenta stock si la producción nace como Completado.
                if (produccion.Estado == "Completado" && produccion.ProductoId.HasValue)
                {
                    var producto = await _context.Productos.FindAsync(produccion.ProductoId.Value);

                    if (producto != null)
                    {
                        producto.Stock += (int)produccion.CantidadResultanteKg;

                        var movimiento = new MovimientoInventario
                        {
                            ProductoId = producto.Id,
                            TipoMovimiento = "Entrada",
                            Cantidad = (int)produccion.CantidadResultanteKg,
                            FechaMovimiento = DateTime.Now,
                            Observacion = $"Entrada automática por producción #{produccion.Id}"
                        };

                        _context.MovimientosInventario.Add(movimiento);
                        _context.Productos.Update(producto);

                        await _context.SaveChangesAsync();
                    }
                }

                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Producciones", "Crear", produccion.Id,
                    $"Se registró la producción #{produccion.Id} para el lote {produccion.LoteId}.");
                TempData["Success"] = "Producción registrada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            CargarCombos(produccion.LoteId, produccion.ProductoId);
            return View(produccion);
        }

        // GET: Producciones/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produccion = await _context.Producciones.FindAsync(id);

            if (produccion == null)
            {
                return NotFound();
            }

            CargarCombos(produccion.LoteId, produccion.ProductoId);

            return View(produccion);
        }

        // POST: Producciones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Produccion produccion)
        {
            if (id != produccion.Id)
            {
                return NotFound();
            }

            var produccionOriginal = await _context.Producciones
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produccionOriginal == null)
            {
                return NotFound();
            }

            if (produccion.CantidadResultanteKg > produccion.CantidadProcesadaKg)
            {
                ModelState.AddModelError(nameof(Produccion.CantidadResultanteKg),
                    "La cantidad resultante no puede superar la cantidad procesada.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Si la producción pasa a Completado por primera vez, se suma stock.
                    if (produccionOriginal.Estado != "Completado" &&
                        produccion.Estado == "Completado" &&
                        produccion.ProductoId.HasValue)
                    {
                        var producto = await _context.Productos.FindAsync(produccion.ProductoId.Value);

                        if (producto != null)
                        {
                            producto.Stock += (int)produccion.CantidadResultanteKg;

                            var movimiento = new MovimientoInventario
                            {
                                ProductoId = producto.Id,
                                TipoMovimiento = "Entrada",
                                Cantidad = (int)produccion.CantidadResultanteKg,
                                FechaMovimiento = DateTime.Now,
                                Observacion = $"Entrada automática por producción #{produccion.Id}"
                            };

                            _context.MovimientosInventario.Add(movimiento);
                            _context.Productos.Update(producto);
                        }
                    }

                    _context.Update(produccion);
                    await _context.SaveChangesAsync();
                    await AuditoriaHelper.RegistrarAsync(
                        _context, User, "Producciones", "Editar", produccion.Id,
                        $"Se actualizó la producción #{produccion.Id}.");
                    TempData["Success"] = "Producción actualizada correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProduccionExists(produccion.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            CargarCombos(produccion.LoteId, produccion.ProductoId);
            return View(produccion);
        }

        // GET: Producciones/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produccion = await _context.Producciones
                .Include(p => p.Lote)
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (produccion == null)
            {
                return NotFound();
            }

            return View(produccion);
        }

        // POST: Producciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var produccion = await _context.Producciones
                    .Include(p => p.Producto)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (produccion == null)
                {
                    return NotFound();
                }

                // Si la producción completada aumentó el inventario, se revierte ese movimiento.
                if (produccion.Estado == "Completado" && produccion.Producto != null)
                {
                    var cantidadARevertir = (int)produccion.CantidadResultanteKg;

                    if (produccion.Producto.Stock < cantidadARevertir)
                    {
                        TempData["Error"] =
                            "No se puede eliminar la producción porque parte de su inventario ya fue utilizado.";
                        return RedirectToAction(nameof(Index));
                    }

                    produccion.Producto.Stock -= cantidadARevertir;

                    var movimientos = await _context.MovimientosInventario
                        .Where(m => m.ProductoId == produccion.ProductoId &&
                                    m.TipoMovimiento == "Entrada" &&
                                    m.Observacion == $"Entrada automática por producción #{produccion.Id}")
                        .ToListAsync();

                    _context.MovimientosInventario.RemoveRange(movimientos);
                }

                var trazabilidades = await _context.Trazabilidades
                    .Where(t => t.ProduccionId == id)
                    .ToListAsync();

                _context.Trazabilidades.RemoveRange(trazabilidades);
                _context.Producciones.Remove(produccion);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Producciones", "Eliminar", id,
                    $"Se eliminó la producción #{id}.");
                TempData["Success"] = "Producción eliminada correctamente.";
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] =
                    "No fue posible eliminar la producción porque todavía tiene información relacionada.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void CargarCombos(int? loteId = null, int? productoId = null)
        {
            ViewBag.LoteId = new SelectList(
                _context.Lotes.OrderBy(l => l.CodigoLote),
                "Id",
                "CodigoLote",
                loteId
            );

            ViewBag.ProductoId = new SelectList(
                _context.Productos.OrderBy(p => p.Nombre),
                "Id",
                "Nombre",
                productoId
            );
        }

        private bool ProduccionExists(int id)
        {
            return _context.Producciones.Any(e => e.Id == id);
        }
    }
}