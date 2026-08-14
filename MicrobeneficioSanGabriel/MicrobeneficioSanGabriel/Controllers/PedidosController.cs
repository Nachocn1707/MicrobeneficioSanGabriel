using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Constants;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;
using MicrobeneficioSanGabriel.ViewModels;
using System.Text.Json;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Vendedor,Cliente")]
    public class PedidosController : Controller
    {
        private const string PedidoPendienteSessionKey = "PedidoPendienteConfirmacion";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPedidoInventarioService _pedidoInventarioService;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPedidoInventarioService pedidoInventarioService,
            ILogger<PedidosController> logger)
        {
            _context = context;
            _userManager = userManager;
            _pedidoInventarioService = pedidoInventarioService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Producto)
                .AsNoTracking()
                .AsQueryable();

            if (User.IsInRole("Cliente"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    return Challenge();
                }

                var correo = (usuario.Email ?? string.Empty).Trim().ToLowerInvariant();
                pedidos = pedidos.Where(p =>
                    p.ClienteId == usuario.Id ||
                    (p.ClienteId == null && p.ClienteCorreo != null && p.ClienteCorreo.ToLower() == correo));
            }

            return View(await pedidos
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync());
        }

        [Authorize(Roles = "Administrador,Cliente")]
        public async Task<IActionResult> Create(int? productoId)
        {
            var pedido = new Pedido
            {
                Cantidad = 1m,
                Estado = EstadosPedido.Pendiente,
                EstadoPago = EstadosPago.Pendiente,
                FechaPedido = DateTime.UtcNow.AddHours(-6)
            };

            if (User.IsInRole("Cliente"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    return Challenge();
                }

                pedido.ClienteId = usuario.Id;
                pedido.ClienteNombre = usuario.NombreCompleto;
                pedido.ClienteCorreo = usuario.Email?.Trim().ToLowerInvariant();
                pedido.ClienteTelefono = SoloDigitos(usuario.PhoneNumber);
            }

            CargarProductos(productoId);

            if (productoId.HasValue)
            {
                var producto = await _context.Productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == productoId.Value && p.Activo && p.Stock > 0);

                if (producto != null)
                {
                    pedido.ProductoId = producto.Id;
                    ViewBag.ProductoNombre = producto.Nombre;
                }
                else
                {
                    TempData["Warning"] = "El producto seleccionado no está disponible. Seleccione otro producto activo con stock.";
                }
            }

            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Cliente")]
        public async Task<IActionResult> Create(Pedido pedido)
        {
            var esSolicitudAjax = EsSolicitudAjax();

            pedido.ClienteNombre = pedido.ClienteNombre?.Trim() ?? string.Empty;
            pedido.ClienteCorreo = string.IsNullOrWhiteSpace(pedido.ClienteCorreo)
                ? null
                : pedido.ClienteCorreo.Trim().ToLowerInvariant();
            pedido.ClienteTelefono = SoloDigitos(pedido.ClienteTelefono);
            pedido.Estado = EstadosPedido.Pendiente;
            pedido.EstadoPago = EstadosPago.Pendiente;
            pedido.MetodoPago = "Sin definir";
            pedido.FechaPedido = DateTime.UtcNow.AddHours(-6);
            pedido.InventarioAplicado = false;

            if (User.IsInRole("Cliente"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    return Challenge();
                }

                pedido.ClienteId = usuario.Id;
                pedido.ClienteNombre = usuario.NombreCompleto;
                pedido.ClienteCorreo = usuario.Email?.Trim().ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(usuario.PhoneNumber))
                {
                    pedido.ClienteTelefono = SoloDigitos(usuario.PhoneNumber);
                }

                ModelState.Remove(nameof(Pedido.ClienteId));
                ModelState.Remove(nameof(Pedido.ClienteNombre));
                ModelState.Remove(nameof(Pedido.ClienteCorreo));
                ModelState.Remove(nameof(Pedido.ClienteTelefono));
                ModelState.Remove(nameof(Pedido.Estado));
                ModelState.Remove(nameof(Pedido.EstadoPago));
                ModelState.Remove(nameof(Pedido.MetodoPago));
            }

            if (pedido.ClienteTelefono.Length != 8)
            {
                ModelState.AddModelError(nameof(Pedido.ClienteTelefono),
                    "El teléfono debe tener 8 dígitos, por ejemplo 88888888.");
            }

            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pedido.ProductoId && p.Activo);

            if (producto == null)
            {
                ModelState.AddModelError(nameof(Pedido.ProductoId), "El producto seleccionado no existe o está inactivo.");
            }
            else if (pedido.Cantidad > producto.Stock)
            {
                ModelState.AddModelError(nameof(Pedido.Cantidad),
                    $"No hay suficiente stock disponible. Máximo: {producto.Stock:N2} kg.");
            }

            if (!ModelState.IsValid)
            {
                if (esSolicitudAjax)
                {
                    var errores = ModelState
                        .Where(item => item.Value?.Errors.Count > 0)
                        .ToDictionary(
                            item => item.Key,
                            item => item.Value!.Errors
                                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                    ? "El valor ingresado no es válido."
                                    : error.ErrorMessage)
                                .ToArray());

                    return BadRequest(new
                    {
                        success = false,
                        message = "Revise los datos marcados antes de continuar al pago.",
                        errors = errores
                    });
                }

                CargarProductos(pedido.ProductoId);
                return View(pedido);
            }

            // El pedido aún no se almacena. Se conserva temporalmente en la sesión
            // hasta que el usuario confirme el método de pago.
            GuardarPedidoPendiente(new PedidoPendienteSession
            {
                ClienteNombre = pedido.ClienteNombre,
                ClienteId = pedido.ClienteId,
                ClienteCorreo = pedido.ClienteCorreo,
                ClienteTelefono = pedido.ClienteTelefono,
                ProductoId = pedido.ProductoId!.Value,
                Cantidad = pedido.Cantidad,
                Observacion = pedido.Observacion,
                FechaCreacion = DateTime.UtcNow.AddHours(-6)
            });

            if (esSolicitudAjax)
            {
                var cultura = System.Globalization.CultureInfo.GetCultureInfo("es-CR");
                var subtotal = Math.Round(producto!.Precio * pedido.Cantidad, 2, MidpointRounding.AwayFromZero);
                var iva = Math.Round(subtotal * 0.13m, 2, MidpointRounding.AwayFromZero);
                var total = Math.Round(subtotal + iva, 2, MidpointRounding.AwayFromZero);

                return Json(new
                {
                    success = true,
                    message = "El pedido está listo para seleccionar el método de pago.",
                    pedidoId = (int?)null,
                    codigoPedido = "Pedido nuevo",
                    producto = producto.Nombre,
                    cantidad = pedido.Cantidad.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    fecha = DateTime.UtcNow.AddHours(-6).ToString("dd MMM yyyy", cultura),
                    subtotalDisplay = $"₡{subtotal.ToString("N0", cultura)}",
                    ivaDisplay = $"₡{iva.ToString("N0", cultura)}",
                    totalDisplay = $"₡{total.ToString("N0", cultura)}",
                    redirectUrl = Url.Action(nameof(Index))
                });
            }

            TempData["Info"] = "Revise el resumen y confirme el método de pago. El pedido todavía no ha sido almacenado.";
            return RedirectToAction(nameof(Pago));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();
            if (User.IsInRole("Cliente") && !await PedidoPerteneceAlClienteAsync(pedido)) return Forbid();

            ViewBag.HistorialEstados = await _context.PedidoEstadoHistoriales
                .AsNoTracking()
                .Where(h => h.PedidoId == pedido.Id)
                .OrderBy(h => h.FechaCambio)
                .ThenBy(h => h.Id)
                .ToListAsync();

            return View(pedido);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var pedido = await _context.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return NotFound();
            pedido.ClienteTelefono = SoloDigitos(pedido.ClienteTelefono);
            ViewBag.FechaPedidoOriginal = pedido.FechaPedido;

            CargarProductos(pedido.ProductoId, incluirInactivo: true);
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, Pedido datos)
        {
            if (id != datos.Id) return NotFound();

            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return NotFound();

            if (datos.RowVersion.Length > 0)
            {
                _context.Entry(pedido).Property(p => p.RowVersion).OriginalValue = datos.RowVersion;
            }

            datos.ClienteNombre = datos.ClienteNombre?.Trim() ?? string.Empty;
            datos.ClienteTelefono = SoloDigitos(datos.ClienteTelefono);
            ViewBag.FechaPedidoOriginal = pedido.FechaPedido;

            if (datos.FechaPedido.Date < DateTime.UtcNow.AddHours(-6).Date &&
                datos.FechaPedido.Date != pedido.FechaPedido.Date)
            {
                ModelState.AddModelError(nameof(Pedido.FechaPedido),
                    "No puede seleccionar una fecha anterior a hoy.");
            }

            if (!EstadosPedido.EsValido(datos.Estado))
            {
                ModelState.AddModelError(nameof(Pedido.Estado), "Seleccione un estado válido.");
            }

            if (datos.ClienteTelefono.Length != 8)
            {
                ModelState.AddModelError(nameof(Pedido.ClienteTelefono), "El teléfono debe contener 8 dígitos.");
            }

            if (!datos.ProductoId.HasValue)
            {
                ModelState.AddModelError(nameof(Pedido.ProductoId), "Debe seleccionar un producto.");
            }

            var tieneFactura = await _context.Facturas.AnyAsync(f => f.PedidoId == id);
            if (tieneFactura && (pedido.ProductoId != datos.ProductoId || pedido.Cantidad != datos.Cantidad))
            {
                ModelState.AddModelError(string.Empty,
                    "No se puede cambiar el producto o la cantidad porque el pedido ya tiene una factura asociada.");
            }

            if (!ModelState.IsValid)
            {
                CargarProductos(datos.ProductoId, incluirInactivo: true);
                return View(datos);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var estadoAnterior = pedido.Estado;
                var errorInventario = await _pedidoInventarioService.ActualizarPedidoAsync(
                    pedido, datos.ProductoId!.Value, datos.Cantidad, datos.Estado);

                if (errorInventario != null)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, errorInventario);
                    CargarProductos(datos.ProductoId, incluirInactivo: true);
                    return View(datos);
                }

                pedido.ClienteNombre = datos.ClienteNombre;
                pedido.ClienteTelefono = datos.ClienteTelefono;
                pedido.FechaPedido = datos.FechaPedido;
                pedido.Observacion = string.IsNullOrWhiteSpace(datos.Observacion) ? null : datos.Observacion.Trim();

                if (!string.Equals(estadoAnterior, pedido.Estado, StringComparison.OrdinalIgnoreCase))
                {
                    await PedidoHistorialHelper.RegistrarAsync(
                        _context, User, pedido, estadoAnterior, pedido.Estado,
                        "Estado actualizado desde la edición administrativa.");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await AuditoriaHelper.RegistrarAsync(_context, User, "Pedidos", "Editar", pedido.Id,
                    $"Se actualizó el pedido #{pedido.Id}. Estado: {estadoAnterior} a {pedido.Estado}.", _logger);
                TempData["Success"] = "Pedido actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Conflicto de concurrencia al editar el pedido {PedidoId}.", id);
                ModelState.AddModelError(string.Empty,
                    "El inventario cambió mientras se procesaba la solicitud. Revise los datos e inténtelo nuevamente.");
                CargarProductos(datos.ProductoId, incluirInactivo: true);
                return View(datos);
            }
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> EditCliente(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();
            if (!await PedidoPerteneceAlClienteAsync(pedido)) return Forbid();

            if (pedido.Estado != EstadosPedido.Pendiente)
            {
                TempData["Error"] = "Solo puede modificar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            pedido.ClienteTelefono = SoloDigitos(pedido.ClienteTelefono);
            CargarProductos(pedido.ProductoId);
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> EditCliente(int id, Pedido datos)
        {
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return NotFound();
            if (!await PedidoPerteneceAlClienteAsync(pedido)) return Forbid();

            if (pedido.Estado != EstadosPedido.Pendiente)
            {
                TempData["Error"] = "Solo puede modificar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == datos.ProductoId && p.Activo);

            if (producto == null)
            {
                ModelState.AddModelError(nameof(Pedido.ProductoId), "El producto seleccionado no está disponible.");
            }
            else if (datos.Cantidad > producto.Stock)
            {
                ModelState.AddModelError(nameof(Pedido.Cantidad),
                    $"No hay suficiente stock. Disponible: {producto.Stock:N2} kg.");
            }

            var telefono = SoloDigitos(datos.ClienteTelefono);
            if (telefono.Length != 8)
            {
                ModelState.AddModelError(nameof(Pedido.ClienteTelefono), "El teléfono debe contener 8 dígitos.");
            }

            ModelState.Remove(nameof(Pedido.ClienteNombre));
            ModelState.Remove(nameof(Pedido.ClienteCorreo));
            ModelState.Remove(nameof(Pedido.Estado));
            ModelState.Remove(nameof(Pedido.EstadoPago));
            ModelState.Remove(nameof(Pedido.MetodoPago));

            if (ModelState.IsValid)
            {
                pedido.ProductoId = datos.ProductoId;
                pedido.ProductoNombre = producto!.Nombre;
                pedido.PrecioUnitario = producto.Precio;
                pedido.Cantidad = datos.Cantidad;
                pedido.ClienteTelefono = telefono;
                pedido.Observacion = string.IsNullOrWhiteSpace(datos.Observacion) ? null : datos.Observacion.Trim();

                await _context.SaveChangesAsync();
                TempData["Success"] = "Pedido actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            datos.ClienteNombre = pedido.ClienteNombre;
            datos.ClienteCorreo = pedido.ClienteCorreo;
            datos.ClienteTelefono = telefono;
            datos.Estado = pedido.Estado;
            datos.FechaPedido = pedido.FechaPedido;
            CargarProductos(datos.ProductoId);
            return View(datos);
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Cancelar(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();
            if (!await PedidoPerteneceAlClienteAsync(pedido)) return Forbid();

            if (pedido.Estado != EstadosPedido.Pendiente)
            {
                TempData["Error"] = "Solo puede cancelar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            return View(pedido);
        }

        [HttpPost, ActionName("Cancelar")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> CancelarConfirmado(int id)
        {
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return NotFound();
            if (!await PedidoPerteneceAlClienteAsync(pedido)) return Forbid();

            if (pedido.Estado != EstadosPedido.Pendiente)
            {
                TempData["Error"] = "Solo puede cancelar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            var estadoAnterior = pedido.Estado;
            pedido.Estado = EstadosPedido.Cancelado;
            await PedidoHistorialHelper.RegistrarAsync(
                _context, User, pedido, estadoAnterior, pedido.Estado,
                "El cliente canceló el pedido.");
            await _context.SaveChangesAsync();
            TempData["Success"] = "Pedido cancelado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
            return pedido == null ? NotFound() : View(pedido);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return RedirectToAction(nameof(Index));

            if (await _context.Facturas.AnyAsync(f => f.PedidoId == id))
            {
                TempData["Error"] = "No se puede eliminar el pedido porque tiene una factura asociada. Anule la factura y conserve el historial.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var error = await _pedidoInventarioService.PrepararEliminacionAsync(pedido);
                if (error != null)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = error;
                    return RedirectToAction(nameof(Index));
                }

                _context.Pedidos.Remove(pedido);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await AuditoriaHelper.RegistrarAsync(_context, User, "Pedidos", "Eliminar", id,
                    $"Se eliminó el pedido #{id} y se ajustó el inventario cuando correspondía.", _logger);
                TempData["Success"] = "Pedido eliminado correctamente.";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Conflicto de concurrencia al eliminar el pedido {PedidoId}.", id);
                TempData["Error"] = "El inventario cambió. Actualice la página antes de intentar eliminar nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador,Vendedor")]
        public async Task<IActionResult> CambiarEstado(int? id)
        {
            if (id == null) return NotFound();
            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            if (User.IsInRole("Vendedor") &&
                !EstadosPago.EsPagado(pedido.EstadoPago))
            {
                TempData["Error"] =
                    "El vendedor solo puede cambiar el estado cuando el pago esté completado.";

                return RedirectToAction(nameof(Index));
            }

            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Vendedor")]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return NotFound();

            if (User.IsInRole("Vendedor") &&
                !EstadosPago.EsPagado(pedido.EstadoPago))
            {
                TempData["Error"] =
                    "El vendedor solo puede cambiar el estado cuando el pago esté completado.";

                return RedirectToAction(nameof(Index));
            }

            if (!EstadosPedido.EsValido(estado))
            {
                TempData["Error"] = "El estado seleccionado no es válido.";
                return RedirectToAction(nameof(CambiarEstado), new { id });
            }

            var estadoAnterior = pedido.Estado;
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var error = await _pedidoInventarioService.CambiarEstadoAsync(pedido, estado);
                if (error != null)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = error;
                    return RedirectToAction(nameof(CambiarEstado), new { id });
                }

                if (!string.Equals(estadoAnterior, pedido.Estado, StringComparison.OrdinalIgnoreCase))
                {
                    await PedidoHistorialHelper.RegistrarAsync(
                        _context, User, pedido, estadoAnterior, pedido.Estado,
                        "Cambio de estado operativo del pedido.");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await AuditoriaHelper.RegistrarAsync(_context, User, "Pedidos", "Cambiar estado", pedido.Id,
                    $"El pedido cambió de {estadoAnterior} a {pedido.Estado}. El pago se mantiene en {pedido.EstadoPago}.", _logger);
                TempData["Success"] = "Estado del pedido actualizado correctamente.";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Conflicto de inventario al cambiar el estado del pedido {PedidoId}.", id);
                TempData["Error"] = "El stock cambió mientras se procesaba el pedido. Inténtelo nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador,Cliente")]
        public async Task<IActionResult> Pago(int? id)
        {
            // Cuando se llega desde "Mis pedidos", el registro ya existe y solo se
            // actualizará el método de pago. Cuando no hay id, se usa el pedido temporal.
            if (id.HasValue)
            {
                var pedidoExistente = await _context.Pedidos
                    .Include(p => p.Producto)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id.Value);

                if (pedidoExistente == null) return NotFound();
                if (User.IsInRole("Cliente") && !await PedidoPerteneceAlClienteAsync(pedidoExistente)) return Forbid();
                if (pedidoExistente.Estado == EstadosPedido.Cancelado)
                {
                    TempData["Error"] = "No se puede registrar pago para un pedido cancelado.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.EsPedidoPendiente = false;
                ViewBag.Total = pedidoExistente.SubtotalMostrar;
                return View(pedidoExistente);
            }

            var pendiente = ObtenerPedidoPendiente();
            if (pendiente == null)
            {
                TempData["Warning"] = "No hay un pedido pendiente de confirmación.";
                return RedirectToAction("Index", "Productos");
            }

            if (User.IsInRole("Cliente"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null) return Challenge();
                if (!string.Equals(pendiente.ClienteId, usuario.Id, StringComparison.Ordinal)) return Forbid();
            }

            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pendiente.ProductoId && p.Activo);

            if (producto == null || producto.Stock < pendiente.Cantidad)
            {
                EliminarPedidoPendiente();
                TempData["Error"] = "El producto ya no está disponible o no tiene suficiente inventario. Vuelva a realizar el pedido.";
                return RedirectToAction("Index", "Productos");
            }

            var pedidoTemporal = new Pedido
            {
                Id = 0,
                ClienteNombre = pendiente.ClienteNombre,
                ClienteId = pendiente.ClienteId,
                ClienteCorreo = pendiente.ClienteCorreo,
                ClienteTelefono = pendiente.ClienteTelefono,
                ProductoId = pendiente.ProductoId,
                Producto = producto,
                ProductoNombre = producto.Nombre,
                PrecioUnitario = producto.Precio,
                Cantidad = pendiente.Cantidad,
                Observacion = pendiente.Observacion,
                FechaPedido = pendiente.FechaCreacion,
                Estado = EstadosPedido.Pendiente,
                EstadoPago = EstadosPago.Pendiente,
                MetodoPago = "Sin definir",
                InventarioAplicado = false
            };

            ViewBag.EsPedidoPendiente = true;
            ViewBag.Total = producto.Precio * pendiente.Cantidad;
            return View(pedidoTemporal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Cliente")]
        public async Task<IActionResult> Pago(int? pedidoId, string metodoPago)
        {
            var esSolicitudAjax = EsSolicitudAjax();
            var metodosPermitidos = new[] { "SINPE Móvil", "Efectivo" };
            if (!metodosPermitidos.Contains(metodoPago))
            {
                const string mensaje = "Seleccione un método de pago válido.";
                if (esSolicitudAjax)
                {
                    return BadRequest(new { success = false, message = mensaje });
                }

                TempData["Error"] = mensaje;
                return pedidoId.HasValue
                    ? RedirectToAction(nameof(Pago), new { id = pedidoId.Value })
                    : RedirectToAction(nameof(Pago));
            }

            // Pedido existente: conserva el flujo para la opción "Realizar pago"
            // que aparece en la lista de pedidos del cliente.
            if (pedidoId.HasValue && pedidoId.Value > 0)
            {
                var pedidoExistente = await _context.Pedidos
                    .Include(p => p.Producto)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId.Value);

                if (pedidoExistente == null)
                {
                    return esSolicitudAjax
                        ? NotFound(new { success = false, message = "El pedido indicado no existe." })
                        : NotFound();
                }

                if (User.IsInRole("Cliente") && !await PedidoPerteneceAlClienteAsync(pedidoExistente))
                {
                    return esSolicitudAjax
                        ? StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "No tiene permiso para pagar este pedido." })
                        : Forbid();
                }

                if (pedidoExistente.Estado == EstadosPedido.Cancelado)
                {
                    const string mensaje = "No se puede registrar pago para un pedido cancelado.";
                    if (esSolicitudAjax)
                    {
                        return BadRequest(new { success = false, message = mensaje });
                    }

                    TempData["Error"] = mensaje;
                    return RedirectToAction(nameof(Index));
                }

                pedidoExistente.MetodoPago = metodoPago;
                pedidoExistente.EstadoPago = EstadosPago.PendientePago;
                await SincronizarEstadoPagoFacturasAsync(pedidoExistente.Id, pedidoExistente.EstadoPago);
                await _context.SaveChangesAsync();

                await AuditoriaHelper.RegistrarAsync(_context, User, "Pedidos", "Registrar método de pago",
                    pedidoExistente.Id, $"Método: {metodoPago}. Estado: {pedidoExistente.EstadoPago}.", _logger);

                if (esSolicitudAjax)
                {
                    return CrearRespuestaPagoAjax(pedidoExistente);
                }

                return metodoPago == "SINPE Móvil"
                    ? RedirectToAction(nameof(PagoConfirmado), new { id = pedidoExistente.Id })
                    : RedirectToAction(nameof(Details), new { id = pedidoExistente.Id });
            }

            // Pedido nuevo: se lee de la sesión y recién aquí se inserta en la base de datos.
            var pendiente = ObtenerPedidoPendiente();
            if (pendiente == null)
            {
                const string mensaje = "La información temporal del pedido venció. Vuelva a realizarlo.";
                if (esSolicitudAjax)
                {
                    return BadRequest(new { success = false, message = mensaje });
                }

                TempData["Error"] = mensaje;
                return RedirectToAction("Index", "Productos");
            }

            if (User.IsInRole("Cliente"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    return esSolicitudAjax
                        ? Unauthorized(new { success = false, message = "La sesión del usuario venció. Inicie sesión nuevamente." })
                        : Challenge();
                }

                if (!string.Equals(pendiente.ClienteId, usuario.Id, StringComparison.Ordinal))
                {
                    return esSolicitudAjax
                        ? StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "No tiene permiso para registrar este pago." })
                        : Forbid();
                }
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == pendiente.ProductoId && p.Activo);

            if (producto == null || producto.Stock < pendiente.Cantidad)
            {
                EliminarPedidoPendiente();
                const string mensaje = "El producto ya no está disponible o no cuenta con suficiente inventario.";
                if (esSolicitudAjax)
                {
                    return BadRequest(new { success = false, message = mensaje });
                }

                TempData["Error"] = mensaje;
                return RedirectToAction("Index", "Productos");
            }

            var pedido = new Pedido
            {
                ClienteNombre = pendiente.ClienteNombre,
                ClienteId = pendiente.ClienteId,
                ClienteCorreo = pendiente.ClienteCorreo,
                ClienteTelefono = pendiente.ClienteTelefono,
                ProductoId = pendiente.ProductoId,
                ProductoNombre = producto.Nombre,
                PrecioUnitario = producto.Precio,
                Cantidad = pendiente.Cantidad,
                Observacion = pendiente.Observacion,
                FechaPedido = DateTime.UtcNow.AddHours(-6),
                Estado = EstadosPedido.Pendiente,
                EstadoPago = EstadosPago.PendientePago,
                MetodoPago = metodoPago,
                InventarioAplicado = false
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();
            await PedidoHistorialHelper.RegistrarAsync(
                _context, User, pedido, null, EstadosPedido.Pendiente,
                "Pedido registrado en el sistema.");
            await _context.SaveChangesAsync();
            EliminarPedidoPendiente();

            await AuditoriaHelper.RegistrarAsync(_context, User, "Pedidos", "Crear", pedido.Id,
                $"Se registró el pedido PP-{pedido.Id:0000} para {pedido.ClienteNombre}, con método {metodoPago}.", _logger);

            pedido.Producto = producto;
            if (esSolicitudAjax)
            {
                return CrearRespuestaPagoAjax(pedido, redirigirAlListado: true);
            }

            return metodoPago == "SINPE Móvil"
                ? RedirectToAction(nameof(PagoConfirmado), new { id = pedido.Id })
                : RedirectToAction(nameof(Details), new { id = pedido.Id });
        }

        [Authorize(Roles = "Administrador,Cliente")]
        public async Task<IActionResult> PagoConfirmado(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();
            if (User.IsInRole("Cliente") && !await PedidoPerteneceAlClienteAsync(pedido)) return Forbid();

            if (!string.Equals(pedido.MetodoPago, "SINPE Móvil", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Details), new { id = pedido.Id });
            }

            ViewBag.Total = pedido.SubtotalMostrar;
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Cliente")]
        public async Task<IActionResult> NoContinuarPago(int? pedidoId)
        {
            // Si el pedido todavía está en sesión, basta con descartarlo: nunca llegó
            // a insertarse en la base de datos.
            if (!pedidoId.HasValue || pedidoId.Value <= 0)
            {
                EliminarPedidoPendiente();
                TempData["Info"] = "La solicitud fue descartada. El pedido no se almacenó.";
                return RedirectToAction("Index", "Productos");
            }

            // Compatibilidad con pedidos antiguos o con la opción "Realizar pago".
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId.Value);
            if (pedido == null) return NotFound();
            if (User.IsInRole("Cliente") && !await PedidoPerteneceAlClienteAsync(pedido)) return Forbid();

            if (pedido.Estado == EstadosPedido.Pendiente && !pedido.InventarioAplicado)
            {
                var estadoAnterior = pedido.Estado;
                pedido.Estado = EstadosPedido.Cancelado;
                await PedidoHistorialHelper.RegistrarAsync(
                    _context, User, pedido, estadoAnterior, pedido.Estado,
                    "Pedido cancelado desde la pantalla de método de pago.");
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(_context, User, "Pedidos", "Cancelar",
                    pedido.Id, "El pedido se canceló desde la pantalla de método de pago.", _logger);
                TempData["Success"] = $"El pedido PP-{pedido.Id:0000} fue cancelado.";
            }
            else
            {
                TempData["Warning"] = "El pedido ya no puede cancelarse desde esta pantalla.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador,Vendedor")]
        public async Task<IActionResult> CambiarEstadoPago(int? id)
        {
            if (id == null) return NotFound();
            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
            return pedido == null ? NotFound() : View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Vendedor")]
        public async Task<IActionResult> CambiarEstadoPago(int id, string estadoPago)
        {
            if (!EstadosPago.Editables.Contains(estadoPago))
            {
                TempData["Error"] = "El estado de pago seleccionado no es válido.";
                return RedirectToAction(nameof(Index));
            }

            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return NotFound();

            var estadoAnterior = pedido.EstadoPago;
            pedido.EstadoPago = estadoPago;
            await SincronizarEstadoPagoFacturasAsync(pedido.Id, estadoPago);
            await _context.SaveChangesAsync();

            await AuditoriaHelper.RegistrarAsync(_context, User, "Pedidos", "Cambiar estado de pago", pedido.Id,
                $"El estado de pago cambió de {estadoAnterior} a {estadoPago}.", _logger);
            TempData["Success"] = $"El estado de pago del pedido #{pedido.Id} se actualizó a {estadoPago}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ConfirmarPago(int id)
        {
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return NotFound();

            pedido.EstadoPago = EstadosPago.PagoCompletado;
            await SincronizarEstadoPagoFacturasAsync(pedido.Id, pedido.EstadoPago);
            await _context.SaveChangesAsync();
            await AuditoriaHelper.RegistrarAsync(_context, User, "Pedidos", "Confirmar pago", pedido.Id,
                "El pago se marcó como completado.", _logger);
            TempData["Success"] = "Pago confirmado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private bool EsSolicitudAjax()
        {
            return string.Equals(
                Request.Headers["X-Requested-With"].ToString(),
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);
        }

        private JsonResult CrearRespuestaPagoAjax(Pedido pedido, bool redirigirAlListado = false)
        {
            const string numeroSinpe = "89546434";
            var codigoPedido = $"PP-{pedido.Id:0000}";
            var textoWhatsapp = Uri.EscapeDataString(
                $"Hola, envío el comprobante de pago por SINPE Móvil correspondiente al pedido {codigoPedido}.");

            return Json(new
            {
                success = true,
                message = "El método de pago fue registrado correctamente.",
                pedidoId = pedido.Id,
                codigoPedido,
                metodoPago = pedido.MetodoPago,
                estadoPago = pedido.EstadoPago,
                sinpeNumero = numeroSinpe,
                whatsappUrl = $"https://wa.me/506{numeroSinpe}?text={textoWhatsapp}",
                redirectUrl = redirigirAlListado ? Url.Action(nameof(Index)) : null
            });
        }

        private async Task SincronizarEstadoPagoFacturasAsync(int pedidoId, string estadoPago)
        {
            var facturas = await _context.Facturas
                .Where(f => f.PedidoId == pedidoId && f.EstadoPago != EstadosPago.Anulada)
                .ToListAsync();

            foreach (var factura in facturas)
            {
                factura.EstadoPago = estadoPago;
            }
        }

        private async Task<bool> PedidoPerteneceAlClienteAsync(Pedido pedido)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return false;

            if (!string.IsNullOrWhiteSpace(pedido.ClienteId))
            {
                return pedido.ClienteId == usuario.Id;
            }

            return string.Equals(
                pedido.ClienteCorreo?.Trim(),
                usuario.Email?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private void CargarProductos(int? productoSeleccionado = null, bool incluirInactivo = false)
        {
            var query = _context.Productos.AsNoTracking().AsQueryable();
            if (!incluirInactivo)
            {
                query = query.Where(p => p.Activo && p.Stock > 0);
            }

            ViewBag.ProductoId = new SelectList(
                query.OrderBy(p => p.Nombre)
                    .AsEnumerable()
                    .Select(p => new
                    {
                        p.Id,
                        Texto = $"{p.Nombre} — ₡{p.Precio:N0} / {p.Stock:N2} kg"
                    }),
                "Id", "Texto", productoSeleccionado);
        }

        private void GuardarPedidoPendiente(PedidoPendienteSession pedido)
        {
            HttpContext.Session.SetString(
                PedidoPendienteSessionKey,
                JsonSerializer.Serialize(pedido));
        }

        private PedidoPendienteSession? ObtenerPedidoPendiente()
        {
            var json = HttpContext.Session.GetString(PedidoPendienteSessionKey);
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<PedidoPendienteSession>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "No se pudo leer el pedido temporal de la sesión.");
                EliminarPedidoPendiente();
                return null;
            }
        }

        private void EliminarPedidoPendiente() =>
            HttpContext.Session.Remove(PedidoPendienteSessionKey);

        private static string SoloDigitos(string? valor) =>
            new((valor ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}