using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Constants;
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
                    .ThenInclude(l => l!.Productor)
                .Include(p => p.Lote)
                    .ThenInclude(l => l!.Finca)
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
            if (!EstadoProduccionValido(produccion.Estado))
            {
                ModelState.AddModelError(nameof(Produccion.Estado), "Seleccione un estado de producción válido.");
            }

            if (produccion.CantidadResultanteKg > produccion.CantidadProcesadaKg)
            {
                ModelState.AddModelError(nameof(Produccion.CantidadResultanteKg),
                    "La cantidad resultante no puede superar la cantidad procesada.");
            }

            await ValidarLoteYCapacidadAsync(produccion);

            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    produccion.FechaProduccion = DateTime.Now;
                    _context.Producciones.Add(produccion);
                    await _context.SaveChangesAsync();

                    if (produccion.Estado == EstadosProduccion.Completado)
                    {
                        var errorInventario = await AplicarEntradaProduccionAsync(produccion);
                        if (errorInventario != null)
                        {
                            await transaction.RollbackAsync();
                            ModelState.AddModelError(nameof(Produccion.Estado), errorInventario);
                            CargarCombos(produccion.LoteId, produccion.ProductoId);
                            return View(produccion);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await AuditoriaHelper.RegistrarAsync(
                        _context, User, "Producciones", "Crear", produccion.Id,
                        $"Se registró la producción #{produccion.Id} para el lote {produccion.LoteId}.");
                    TempData["Success"] = "Producción registrada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty,
                        "El inventario cambió mientras se registraba la producción. Inténtelo nuevamente.");
                }
            }

            CargarCombos(produccion.LoteId, produccion.ProductoId);
            return View(produccion);
        }

        // GET: Producciones/Edit/5
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produccion = await _context.Producciones
                .Include(p => p.Lote)
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produccion == null)
            {
                return NotFound();
            }

            ViewBag.SoloEstado = EsOperadorSoloEstado();
            CargarCombos(produccion.LoteId, produccion.ProductoId);

            return View(produccion);
        }

        // POST: Producciones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> Edit(int id, Produccion produccion)
        {
            if (id != produccion.Id)
            {
                return NotFound();
            }

            var produccionOriginal = await _context.Producciones
                .Include(p => p.Lote)
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produccionOriginal == null)
            {
                return NotFound();
            }

            if (EsOperadorSoloEstado())
            {
                if (!EstadoProduccionValido(produccion.Estado))
                {
                    ModelState.AddModelError(nameof(Produccion.Estado), "Seleccione un estado válido.");
                    ViewBag.SoloEstado = true;
                    CargarCombos(produccionOriginal.LoteId, produccionOriginal.ProductoId);
                    produccionOriginal.Estado = produccion.Estado;
                    return View(produccionOriginal);
                }

                var estadoAnterior = produccionOriginal.Estado;
                var errorEstado = await CambiarEstadoProduccionAsync(produccionOriginal, produccion.Estado);

                if (errorEstado != null)
                {
                    ModelState.AddModelError(nameof(Produccion.Estado), errorEstado);
                    ViewBag.SoloEstado = true;
                    CargarCombos(produccionOriginal.LoteId, produccionOriginal.ProductoId);
                    return View(produccionOriginal);
                }

                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Producciones", "Cambiar estado", produccionOriginal.Id,
                    $"El operador cambió el estado de la producción #{produccionOriginal.Id} de {estadoAnterior} a {produccionOriginal.Estado}.");

                TempData["Success"] = "Estado de producción actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            if (produccion.CantidadResultanteKg > produccion.CantidadProcesadaKg)
            {
                ModelState.AddModelError(nameof(Produccion.CantidadResultanteKg),
                    "La cantidad resultante no puede superar la cantidad procesada.");
            }

            if (!EstadoProduccionValido(produccion.Estado))
            {
                ModelState.AddModelError(nameof(Produccion.Estado), "Seleccione un estado válido.");
            }

            await ValidarLoteYCapacidadAsync(produccion, produccion.Id);

            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var errorInventario = await SincronizarInventarioProduccionAsync(produccionOriginal, produccion);

                    if (errorInventario != null)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError(nameof(Produccion.Estado), errorInventario);
                        ViewBag.SoloEstado = false;
                        CargarCombos(produccion.LoteId, produccion.ProductoId);
                        return View(produccion);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await AuditoriaHelper.RegistrarAsync(
                        _context, User, "Producciones", "Editar", produccionOriginal.Id,
                        $"Se actualizó la producción #{produccionOriginal.Id}.");
                    TempData["Success"] = "Producción actualizada correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    if (!ProduccionExists(produccion.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }
                catch (DbUpdateException)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty,
                        "No fue posible actualizar la producción. Verifique la información relacionada.");
                    ViewBag.SoloEstado = false;
                    CargarCombos(produccion.LoteId, produccion.ProductoId);
                    return View(produccion);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.SoloEstado = false;
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
                    .ThenInclude(l => l!.Productor)
                .Include(p => p.Lote)
                    .ThenInclude(l => l!.Finca)
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

                // Si la producción completada aumentó el inventario, se registra su reversión.
                if (produccion.Estado == EstadosProduccion.Completado)
                {
                    var errorReversion = await RevertirEntradaProduccionAsync(produccion);
                    if (errorReversion != null)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = errorReversion;
                        return RedirectToAction(nameof(Index));
                    }
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

        private bool EsOperadorSoloEstado()
        {
            return User.IsInRole("Operador") && !User.IsInRole("Administrador");
        }

        private static bool EstadoProduccionValido(string? estado)
        {
            return EstadosProduccion.EsValido(estado);
        }

        private async Task<string?> CambiarEstadoProduccionAsync(Produccion produccion, string nuevoEstado)
        {
            if (produccion.Estado == nuevoEstado)
            {
                return null;
            }

            if (produccion.Estado == EstadosProduccion.Completado)
            {
                var errorReversion = await RevertirEntradaProduccionAsync(produccion);
                if (errorReversion != null)
                {
                    return errorReversion;
                }
            }

            produccion.Estado = nuevoEstado;

            if (produccion.Estado == EstadosProduccion.Completado)
            {
                return await AplicarEntradaProduccionAsync(produccion);
            }

            return null;
        }

        private async Task<string?> SincronizarInventarioProduccionAsync(Produccion produccionActual, Produccion datosNuevos)
        {
            var cambiaInventario = produccionActual.Estado != datosNuevos.Estado ||
                                   produccionActual.ProductoId != datosNuevos.ProductoId ||
                                   produccionActual.CantidadResultanteKg != datosNuevos.CantidadResultanteKg;

            if (cambiaInventario && produccionActual.Estado == EstadosProduccion.Completado)
            {
                var errorReversion = await RevertirEntradaProduccionAsync(produccionActual);
                if (errorReversion != null)
                {
                    return errorReversion;
                }
            }

            produccionActual.LoteId = datosNuevos.LoteId;
            produccionActual.FechaProduccion = datosNuevos.FechaProduccion;
            produccionActual.TipoProceso = datosNuevos.TipoProceso?.Trim() ?? string.Empty;
            produccionActual.CantidadProcesadaKg = datosNuevos.CantidadProcesadaKg;
            produccionActual.CantidadResultanteKg = datosNuevos.CantidadResultanteKg;
            produccionActual.Estado = datosNuevos.Estado;
            produccionActual.Observacion = string.IsNullOrWhiteSpace(datosNuevos.Observacion)
                ? null
                : datosNuevos.Observacion.Trim();
            produccionActual.ProductoId = datosNuevos.ProductoId;

            if (cambiaInventario && produccionActual.Estado == EstadosProduccion.Completado)
            {
                return await AplicarEntradaProduccionAsync(produccionActual);
            }

            return null;
        }

        private async Task<string?> AplicarEntradaProduccionAsync(Produccion produccion)
        {
            if (!produccion.ProductoId.HasValue)
            {
                return "Para marcar la producción como completada debe seleccionar un producto resultante.";
            }

            var producto = await _context.Productos.FindAsync(produccion.ProductoId.Value);
            if (producto == null)
            {
                return "El producto resultante seleccionado no existe.";
            }

            var cantidad = produccion.CantidadResultanteKg;
            producto.Stock += cantidad;

            var movimiento = new MovimientoInventario
            {
                ProductoId = producto.Id,
                TipoMovimiento = "Entrada",
                Cantidad = cantidad,
                FechaMovimiento = DateTime.Now,
                Observacion = $"Entrada automática por producción #{produccion.Id}",
                OrigenTipo = OrigenMovimiento.Produccion,
                OrigenId = produccion.Id,
                EsAutomatico = true
            };

            _context.MovimientosInventario.Add(movimiento);
            return null;
        }

        private async Task<string?> RevertirEntradaProduccionAsync(Produccion produccion)
        {
            if (!produccion.ProductoId.HasValue)
            {
                return null;
            }

            var producto = await _context.Productos.FindAsync(produccion.ProductoId.Value);
            if (producto == null)
            {
                return "No se encontró el producto asociado para ajustar el inventario.";
            }

            var cantidad = produccion.CantidadResultanteKg;
            if (producto.Stock < cantidad)
            {
                return "No se puede cambiar el estado porque parte del inventario generado por esta producción ya fue utilizado.";
            }

            producto.Stock -= cantidad;

            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = producto.Id,
                TipoMovimiento = "Salida",
                Cantidad = cantidad,
                FechaMovimiento = DateTime.Now,
                Observacion = $"Reversión automática de producción #{produccion.Id}",
                OrigenTipo = OrigenMovimiento.ReversionProduccion,
                OrigenId = produccion.Id,
                EsAutomatico = true
            });

            return null;
        }


        private async Task ValidarLoteYCapacidadAsync(Produccion produccion, int? excluirProduccionId = null)
        {
            var lote = await _context.Lotes
                .Include(l => l.Finca)
                .Include(l => l.Productor)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == produccion.LoteId);

            if (lote == null)
            {
                ModelState.AddModelError(nameof(Produccion.LoteId), "El lote seleccionado no existe.");
                return;
            }

            if (lote.Estado is EstadosLote.Cancelado or EstadosLote.Finalizado)
            {
                ModelState.AddModelError(nameof(Produccion.LoteId),
                    "No se puede registrar producción para un lote cancelado o finalizado.");
            }

            if (lote.Finca == null || !lote.Finca.Activa || lote.Productor == null || !lote.Productor.Activo)
            {
                ModelState.AddModelError(nameof(Produccion.LoteId),
                    "El lote debe pertenecer a una finca y un productor activos.");
            }

            if (produccion.ProductoId.HasValue &&
                !await _context.Productos.AsNoTracking().AnyAsync(p => p.Id == produccion.ProductoId && p.Activo))
            {
                ModelState.AddModelError(nameof(Produccion.ProductoId),
                    "El producto resultante no existe o está inactivo.");
            }

            var usadasQuery = _context.Producciones
                .AsNoTracking()
                .Where(p => p.LoteId == produccion.LoteId && p.Estado != EstadosProduccion.Cancelado);

            if (excluirProduccionId.HasValue)
            {
                usadasQuery = usadasQuery.Where(p => p.Id != excluirProduccionId.Value);
            }

            var cantidadYaProcesada = await usadasQuery.SumAsync(p => (decimal?)p.CantidadProcesadaKg) ?? 0m;
            if (cantidadYaProcesada + produccion.CantidadProcesadaKg > lote.PesoKg)
            {
                var disponible = Math.Max(0m, lote.PesoKg - cantidadYaProcesada);
                ModelState.AddModelError(nameof(Produccion.CantidadProcesadaKg),
                    $"La suma de producciones supera el peso recibido del lote. Disponible: {disponible:N2} kg.");
            }
        }

        private void CargarCombos(int? loteId = null, int? productoId = null)
        {
            ViewBag.LoteId = new SelectList(
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