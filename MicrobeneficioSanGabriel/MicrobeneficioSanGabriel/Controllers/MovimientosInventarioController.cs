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
    public class MovimientosInventarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovimientosInventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var movimientos = await _context.MovimientosInventario
                .Include(m => m.Producto)
                .OrderByDescending(m => m.FechaMovimiento)
                .ToListAsync();

            ViewBag.ProductosStock = await _context.Productos
                .Where(p => p.Activo)
                .OrderByDescending(p => p.Stock)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return View(movimientos);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var movimiento = await _context.MovimientosInventario
                .Include(m => m.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            return movimiento == null ? NotFound() : View(movimiento);
        }

        public IActionResult Create()
        {
            CargarProductos();
            return View(new MovimientoInventario { FechaMovimiento = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProductoId,TipoMovimiento,Cantidad,Observacion")]
            MovimientoInventario movimiento)
        {
            var producto = await _context.Productos.FindAsync(movimiento.ProductoId);
            if (producto == null)
            {
                ModelState.AddModelError(nameof(MovimientoInventario.ProductoId),
                    "El producto seleccionado no existe.");
            }

            if (!EsTipoValido(movimiento.TipoMovimiento))
            {
                ModelState.AddModelError(nameof(MovimientoInventario.TipoMovimiento),
                    "Debe seleccionar Entrada o Salida.");
            }

            if (producto != null && movimiento.TipoMovimiento == "Salida" &&
                producto.Stock < movimiento.Cantidad)
            {
                ModelState.AddModelError(nameof(MovimientoInventario.Cantidad),
                    $"No hay suficiente stock. Disponible: {producto.Stock} kg.");
            }

            if (ModelState.IsValid && producto != null)
            {
                movimiento.FechaMovimiento = DateTime.Now;
                movimiento.Observacion = movimiento.Observacion?.Trim();
                movimiento.EsAutomatico = false;
                movimiento.OrigenTipo = null;
                movimiento.OrigenId = null;

                AplicarMovimiento(producto, movimiento.TipoMovimiento, movimiento.Cantidad);
                _context.MovimientosInventario.Add(movimiento);

                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Inventario", "Crear", movimiento.Id,
                    $"Se registró una {movimiento.TipoMovimiento.ToLower()} de {movimiento.Cantidad} kg para {producto.Nombre}.");

                TempData["Success"] = "Movimiento de inventario registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            CargarProductos(movimiento.ProductoId);
            return View(movimiento);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var movimiento = await _context.MovimientosInventario.FindAsync(id);
            if (movimiento == null) return NotFound();
            CargarProductos(movimiento.ProductoId);
            return View(movimiento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,ProductoId,TipoMovimiento,Cantidad,Observacion,FechaMovimiento")]
            MovimientoInventario movimiento)
        {
            if (id != movimiento.Id) return NotFound();

            var original = await _context.MovimientosInventario
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (original == null) return NotFound();
            if (!EsTipoValido(movimiento.TipoMovimiento))
            {
                ModelState.AddModelError(nameof(MovimientoInventario.TipoMovimiento),
                    "Debe seleccionar Entrada o Salida.");
            }

            var productoOriginal = await _context.Productos.FindAsync(original.ProductoId);
            var productoNuevo = original.ProductoId == movimiento.ProductoId
                ? productoOriginal
                : await _context.Productos.FindAsync(movimiento.ProductoId);

            if (productoOriginal == null || productoNuevo == null)
            {
                ModelState.AddModelError(nameof(MovimientoInventario.ProductoId),
                    "No fue posible encontrar el producto relacionado.");
            }

            if (ModelState.IsValid && productoOriginal != null && productoNuevo != null)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Primero se revierte el efecto del movimiento original.
                    if (!PuedeRevertir(productoOriginal, original.TipoMovimiento, original.Cantidad))
                    {
                        ModelState.AddModelError(string.Empty,
                            "No se puede editar este movimiento porque parte del stock generado ya fue utilizado.");
                    }
                    else
                    {
                        RevertirMovimiento(productoOriginal, original.TipoMovimiento, original.Cantidad);

                        if (movimiento.TipoMovimiento == "Salida" && productoNuevo.Stock < movimiento.Cantidad)
                        {
                            ModelState.AddModelError(nameof(MovimientoInventario.Cantidad),
                                $"No hay suficiente stock. Disponible después de revertir: {productoNuevo.Stock} kg.");
                        }
                        else
                        {
                            AplicarMovimiento(productoNuevo, movimiento.TipoMovimiento, movimiento.Cantidad);
                            movimiento.FechaMovimiento = original.FechaMovimiento;
                            movimiento.Observacion = movimiento.Observacion?.Trim();
                            // Conserva la trazabilidad del origen aunque el movimiento automático sea ajustado.
                            movimiento.EsAutomatico = original.EsAutomatico;
                            movimiento.OrigenTipo = original.OrigenTipo;
                            movimiento.OrigenId = original.OrigenId;

                            _context.MovimientosInventario.Update(movimiento);
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            await AuditoriaHelper.RegistrarAsync(
                                _context, User, "Inventario", "Editar", movimiento.Id,
                                $"Se actualizó el movimiento #{movimiento.Id}.");

                            TempData["Success"] = "Movimiento actualizado y stock recalculado correctamente.";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    await transaction.RollbackAsync();
                    _context.ChangeTracker.Clear();
                }
                catch (DbUpdateException)
                {
                    await transaction.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    ModelState.AddModelError(string.Empty,
                        "No fue posible actualizar el movimiento. Inténtelo nuevamente.");
                }
            }

            CargarProductos(movimiento.ProductoId);
            return View(movimiento);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var movimiento = await _context.MovimientosInventario
                .Include(m => m.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            return movimiento == null ? NotFound() : View(movimiento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movimiento = await _context.MovimientosInventario
                .Include(m => m.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movimiento == null) return NotFound();
            if (movimiento.Producto == null)
            {
                TempData["Error"] = "El movimiento no tiene un producto válido asociado.";
                return RedirectToAction(nameof(Index));
            }

            if (!PuedeRevertir(movimiento.Producto, movimiento.TipoMovimiento, movimiento.Cantidad))
            {
                TempData["Error"] =
                    "No se puede eliminar este movimiento porque parte del stock de entrada ya fue utilizado.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                RevertirMovimiento(movimiento.Producto, movimiento.TipoMovimiento, movimiento.Cantidad);
                _context.MovimientosInventario.Remove(movimiento);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Inventario", "Eliminar", id,
                    $"Se eliminó y revirtió el movimiento #{id}.");

                TempData["Success"] = "Movimiento eliminado y stock revertido correctamente.";
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "No fue posible eliminar el movimiento de inventario.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void CargarProductos(int? seleccionado = null)
        {
            ViewData["ProductoId"] = new SelectList(
                _context.Productos.OrderBy(p => p.Nombre),
                "Id", "Nombre", seleccionado);
        }

        private static bool EsTipoValido(string tipo) =>
            tipo == "Entrada" || tipo == "Salida";

        private static void AplicarMovimiento(Producto producto, string tipo, decimal cantidad)
        {
            if (tipo == "Entrada") producto.Stock += cantidad;
            else producto.Stock -= cantidad;
        }

        private static bool PuedeRevertir(Producto producto, string tipo, decimal cantidad) =>
            tipo != "Entrada" || producto.Stock >= cantidad;

        private static void RevertirMovimiento(Producto producto, string tipo, decimal cantidad)
        {
            if (tipo == "Entrada") producto.Stock -= cantidad;
            else producto.Stock += cantidad;
        }
    }
}
