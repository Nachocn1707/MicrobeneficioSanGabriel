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
            await TrazabilidadProcesoHelper.ReconciliarAsync(_context, User);

            var produccionesQuery = await _context.Producciones
                .Include(p => p.Lote)
                .Include(p => p.Producto)
                .ToListAsync();

            // Garantiza que en la tabla principal de Producción solo se muestre 1 fila por lote (el proceso activo o la etapa más reciente).
            var produccionesUnicasPorLote = produccionesQuery
                .GroupBy(p => p.LoteId)
                .Select(g => g
                    .OrderByDescending(p => string.Equals(p.Estado, EstadosProduccion.EnProceso, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(p => Array.IndexOf(TrazabilidadProcesoHelper.EtapasOrdenadas, TrazabilidadProcesoHelper.EtapasOrdenadas.FirstOrDefault(e => string.Equals(e, p.TipoProceso, StringComparison.OrdinalIgnoreCase)) ?? ""))
                    .ThenByDescending(p => p.FechaProduccion)
                    .ThenByDescending(p => p.Id)
                    .First())
                .OrderByDescending(p => string.Equals(p.Estado, EstadosProduccion.EnProceso, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(p => p.FechaProduccion)
                .ThenByDescending(p => p.Id)
                .ToList();

            return View(produccionesUnicasPorLote);
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

            ViewBag.HistorialLote = await _context.Producciones
                .AsNoTracking()
                .Where(p => p.LoteId == produccion.LoteId)
                .OrderBy(p => p.FechaProduccion)
                .ToListAsync();

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

            var errorSecuencia = await TrazabilidadProcesoHelper.ValidarSecuenciaAsync(_context, produccion);
            if (errorSecuencia != null)
            {
                ModelState.AddModelError(nameof(Produccion.TipoProceso), errorSecuencia);
            }

            if (produccion.CantidadResultanteKg > produccion.CantidadProcesadaKg)
            {
                ModelState.AddModelError(nameof(Produccion.CantidadResultanteKg),
                    "La cantidad resultante no puede superar la cantidad procesada.");
            }

            await ValidarLoteYCapacidadAsync(produccion);

            if (produccion.ProductoId.HasValue)
            {
                var productoSeleccionado = await _context.Productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == produccion.ProductoId.Value);

                if (productoSeleccionado == null)
                {
                    ModelState.AddModelError(nameof(Produccion.ProductoId),
                        "El producto resultante seleccionado no existe.");
                }
                else
                {
                    produccion.ProductoNombre = productoSeleccionado.Nombre;
                }
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    produccion.FechaProduccion = DateTime.UtcNow.AddHours(-6);
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

                    await TrazabilidadProcesoHelper.SincronizarAsync(_context, User, produccion);
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

            ViewBag.SoloEstado = false;
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

            if (produccion.CantidadResultanteKg > produccion.CantidadProcesadaKg)
            {
                ModelState.AddModelError(nameof(Produccion.CantidadResultanteKg),
                    "La cantidad resultante no puede superar la cantidad procesada.");
            }

            if (!EstadoProduccionValido(produccion.Estado))
            {
                ModelState.AddModelError(nameof(Produccion.Estado), "Seleccione un estado válido.");
            }

            if (TrazabilidadProcesoHelper.EsEtapaDeProduccionValida(produccionOriginal.TipoProceso) &&
                TrazabilidadProcesoHelper.EsEtapaDeProduccionValida(produccion.TipoProceso))
            {
                var idxOriginal = Array.IndexOf(TrazabilidadProcesoHelper.EtapasOrdenadas,
                    TrazabilidadProcesoHelper.EtapasOrdenadas.First(e => string.Equals(e, produccionOriginal.TipoProceso, StringComparison.OrdinalIgnoreCase)));
                var idxNuevo = Array.IndexOf(TrazabilidadProcesoHelper.EtapasOrdenadas,
                    TrazabilidadProcesoHelper.EtapasOrdenadas.First(e => string.Equals(e, produccion.TipoProceso, StringComparison.OrdinalIgnoreCase)));

                if (idxNuevo < idxOriginal)
                {
                    ModelState.AddModelError(nameof(Produccion.TipoProceso),
                        $"No se puede devolver el proceso a una etapa anterior ({produccionOriginal.TipoProceso} ➔ {produccion.TipoProceso}). Solo se permite avanzar en la secuencia del proceso.");
                }
                else if (idxNuevo > idxOriginal + 1)
                {
                    var etapaSiguiente = TrazabilidadProcesoHelper.EtapasOrdenadas[idxOriginal + 1];
                    ModelState.AddModelError(nameof(Produccion.TipoProceso),
                        $"No se pueden saltar etapas del proceso ({produccionOriginal.TipoProceso} ➔ {produccion.TipoProceso}). La siguiente etapa correspondiente es {etapaSiguiente}.");
                }
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

                    await TrazabilidadProcesoHelper.SincronizarAsync(_context, User, produccionOriginal);
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

        // POST: Producciones/AvanzarProceso/5
        // Avanza el proceso a la siguiente etapa en la misma producción (o completa si está en la etapa final)
        // manteniendo 1 solo proceso activo por lote en la tabla.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> AvanzarProceso(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var produccion = await _context.Producciones
                    .Include(p => p.Lote)
                    .Include(p => p.Producto)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (produccion == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                if (string.Equals(produccion.Estado, EstadosProduccion.Cancelado, StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "No se puede avanzar una producción cancelada.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.Equals(produccion.Estado, EstadosProduccion.Completado, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(produccion.Estado, "Completada", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();
                    TempData["Info"] = "Esta producción ya se encuentra completada.";
                    return RedirectToAction(nameof(Index));
                }

                if (!TrazabilidadProcesoHelper.EsEtapaDeProduccionValida(produccion.TipoProceso))
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "La producción no tiene una etapa válida para continuar el proceso.";
                    return RedirectToAction(nameof(Index));
                }

                var etapaActual = TrazabilidadProcesoHelper.EtapasOrdenadas.First(e =>
                    string.Equals(e, produccion.TipoProceso, StringComparison.OrdinalIgnoreCase));
                var indiceActual = Array.IndexOf(TrazabilidadProcesoHelper.EtapasOrdenadas, etapaActual);
                var ahora = DateTime.UtcNow.AddHours(-6);

                produccion.FechaProduccion = ahora;

                if (indiceActual >= 0 && indiceActual < TrazabilidadProcesoHelper.EtapasOrdenadas.Length - 1)
                {
                    var etapaSiguiente = TrazabilidadProcesoHelper.EtapasOrdenadas[indiceActual + 1];

                    // Avanzar la etapa dentro del mismo registro de producción
                    produccion.TipoProceso = etapaSiguiente;
                    produccion.Estado = EstadosProduccion.EnProceso;
                    produccion.Observacion = $"Proceso avanzado de {etapaActual} a {etapaSiguiente}.";

                    _context.Producciones.Update(produccion);
                    await _context.SaveChangesAsync();

                    await TrazabilidadProcesoHelper.SincronizarAsync(_context, User, produccion);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await AuditoriaHelper.RegistrarAsync(
                        _context, User, "Producciones", "Avanzar proceso", produccion.Id,
                        $"Se avanzó el proceso del lote {produccion.LoteId} de {etapaActual} a {etapaSiguiente}.");

                    TempData["Success"] = $"El proceso avanzó correctamente de {etapaActual} a {etapaSiguiente} ({ahora:dd/MM/yyyy HH:mm}).";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Si se encuentra en la última etapa (Empaque), se marca la producción como Completada
                    var errorInventario = await CambiarEstadoProduccionAsync(
                        produccion,
                        EstadosProduccion.Completado);

                    if (errorInventario != null)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = errorInventario;
                        return RedirectToAction(nameof(Index));
                    }

                    await TrazabilidadProcesoHelper.SincronizarAsync(_context, User, produccion);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await AuditoriaHelper.RegistrarAsync(
                        _context, User, "Producciones", "Avanzar proceso", produccion.Id,
                        $"Se completó la última etapa ({etapaActual}) de la producción #{produccion.Id}.");

                    TempData["Success"] = $"La producción finalizó la etapa de {etapaActual} y se marcó como Completada ({ahora:dd/MM/yyyy HH:mm}).";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "La producción cambió mientras se actualizaba. Actualice la página e inténtelo nuevamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Ocurrió un error al avanzar el proceso: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
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

                var errorDependencias = await TrazabilidadProcesoHelper.ValidarEliminacionAsync(_context, produccion);
                if (errorDependencias != null)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = errorDependencias;
                    return RedirectToAction(nameof(Index));
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

            if (datosNuevos.ProductoId.HasValue)
            {
                var productoSeleccionado = await _context.Productos.FindAsync(datosNuevos.ProductoId.Value);
                if (productoSeleccionado == null)
                {
                    return "El producto resultante seleccionado no existe.";
                }

                produccionActual.ProductoNombre = productoSeleccionado.Nombre;
            }

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
            produccion.ProductoNombre = producto.Nombre;
            producto.Stock += cantidad;

            var movimiento = new MovimientoInventario
            {
                ProductoId = producto.Id,
                ProductoNombre = producto.Nombre,
                TipoMovimiento = "Entrada",
                Cantidad = cantidad,
                FechaMovimiento = DateTime.UtcNow.AddHours(-6),
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
                // El catálogo fue eliminado y no queda inventario que revertir.
                produccion.ProductoId = null;
                return null;
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
                ProductoNombre = producto.Nombre,
                TipoMovimiento = "Salida",
                Cantidad = cantidad,
                FechaMovimiento = DateTime.UtcNow.AddHours(-6),
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

            if (!TrazabilidadProcesoHelper.EsEtapaDeProduccionValida(produccion.TipoProceso))
            {
                return;
            }

            // En edición se permite actualizar etapa y estado sin bloqueos de capacidad
            if (excluirProduccionId.HasValue)
            {
                return;
            }

            var etapa = TrazabilidadProcesoHelper.EtapasOrdenadas.First(e =>
                string.Equals(e, produccion.TipoProceso, StringComparison.OrdinalIgnoreCase));
            var indiceEtapa = Array.IndexOf(TrazabilidadProcesoHelper.EtapasOrdenadas, etapa);

            // La capacidad se controla por etapa. Sumar Lavado + Secado + Tostado como si
            // consumieran nuevamente el peso original del lote impediría un flujo secuencial real.
            // Lavado consume el peso recibido; las etapas siguientes consumen únicamente la salida
            // completada de la etapa inmediatamente anterior.
            decimal capacidadEtapa;
            string descripcionOrigen;

            if (indiceEtapa == 0)
            {
                capacidadEtapa = lote.PesoKg;
                descripcionOrigen = "peso recibido del lote";
            }
            else
            {
                var etapaAnterior = TrazabilidadProcesoHelper.EtapasOrdenadas[indiceEtapa - 1];
                var salidasAnteriores = _context.Producciones
                    .AsNoTracking()
                    .Where(p => p.LoteId == produccion.LoteId &&
                                p.Estado == EstadosProduccion.Completado &&
                                p.TipoProceso == etapaAnterior);

                if (excluirProduccionId.HasValue)
                {
                    // Es importante al editar y cambiar de etapa: el registro actual todavía tiene
                    // sus valores anteriores en la BD y nunca debe abastecerse a sí mismo.
                    salidasAnteriores = salidasAnteriores.Where(p => p.Id != excluirProduccionId.Value);
                }

                capacidadEtapa = await salidasAnteriores
                    .SumAsync(p => (decimal?)p.CantidadResultanteKg) ?? 0m;
                descripcionOrigen = $"cantidad resultante completada de {etapaAnterior}";
            }

            var usadasMismaEtapa = _context.Producciones
                .AsNoTracking()
                .Where(p => p.LoteId == produccion.LoteId &&
                            p.Estado != EstadosProduccion.Cancelado &&
                            p.TipoProceso == etapa);

            if (excluirProduccionId.HasValue)
            {
                usadasMismaEtapa = usadasMismaEtapa.Where(p => p.Id != excluirProduccionId.Value);
            }

            var cantidadYaProcesada = await usadasMismaEtapa
                .SumAsync(p => (decimal?)p.CantidadProcesadaKg) ?? 0m;
            var disponible = Math.Max(0m, capacidadEtapa - cantidadYaProcesada);

            if (produccion.CantidadProcesadaKg > disponible)
            {
                ModelState.AddModelError(nameof(Produccion.CantidadProcesadaKg),
                    $"La etapa {etapa} supera la {descripcionOrigen}. Disponible para esta etapa: {disponible:N2} kg.");
            }

            // Si una etapa ya alimenta a la siguiente, tampoco puede editarse su rendimiento de
            // forma que deje a los procesos posteriores consumiendo más café del que realmente salió.
            if (produccion.Estado == EstadosProduccion.Completado &&
                indiceEtapa < TrazabilidadProcesoHelper.EtapasOrdenadas.Length - 1)
            {
                var otrasSalidasCompletadas = _context.Producciones
                    .AsNoTracking()
                    .Where(p => p.LoteId == produccion.LoteId &&
                                p.Estado == EstadosProduccion.Completado &&
                                p.TipoProceso == etapa);

                if (excluirProduccionId.HasValue)
                {
                    otrasSalidasCompletadas = otrasSalidasCompletadas.Where(p => p.Id != excluirProduccionId.Value);
                }

                var salidaTotalEtapa = (await otrasSalidasCompletadas
                    .SumAsync(p => (decimal?)p.CantidadResultanteKg) ?? 0m) + produccion.CantidadResultanteKg;

                var etapaSiguiente = TrazabilidadProcesoHelper.EtapasOrdenadas[indiceEtapa + 1];
                var consumidoEtapaSiguiente = await _context.Producciones
                    .AsNoTracking()
                    .Where(p => p.LoteId == produccion.LoteId &&
                                p.Estado != EstadosProduccion.Cancelado &&
                                p.TipoProceso == etapaSiguiente &&
                                (!excluirProduccionId.HasValue || p.Id != excluirProduccionId.Value))
                    .SumAsync(p => (decimal?)p.CantidadProcesadaKg) ?? 0m;

                if (consumidoEtapaSiguiente > salidaTotalEtapa)
                {
                    ModelState.AddModelError(nameof(Produccion.CantidadResultanteKg),
                        $"No puede reducir la salida de {etapa} a {salidaTotalEtapa:N2} kg porque {etapaSiguiente} ya tiene {consumidoEtapaSiguiente:N2} kg registrados.");
                }
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