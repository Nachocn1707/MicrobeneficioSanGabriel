using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Vendedor,Cliente")]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PedidosController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Producto)
                .AsQueryable();

            if (User.IsInRole("Cliente"))
            {
                var usuario = await _userManager.GetUserAsync(User);

                var correo = usuario?.Email;
                var nombre = usuario?.NombreCompleto;

                pedidos = pedidos.Where(p =>
                    p.ClienteCorreo == correo 
                );
            }

            return View(await pedidos
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync());
        }

        public async Task<IActionResult> Create(int? productoId)
        {
            var pedido = new Pedido
            {
                Cantidad = 1,
                Estado = "Pendiente",
                FechaPedido = DateTime.Now
            };

            if (productoId.HasValue)
            {
                var producto = await _context.Productos
                    .FindAsync(productoId.Value);

                if (producto != null)
                {
                    pedido.ProductoId = producto.Id;
                    ViewBag.ProductoNombre = producto.Nombre;
                }
            }
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pedido pedido)
        {
            if (User.IsInRole("Cliente"))
            {
                var usuario = await _userManager.GetUserAsync(User);

                if (usuario != null)
                {
                    pedido.ClienteNombre = usuario.NombreCompleto;
                    pedido.ClienteCorreo = usuario.Email;
                    pedido.ClienteTelefono = usuario.PhoneNumber;
                }

                pedido.Estado = "Pendiente";
                pedido.FechaPedido = DateTime.Now;
            }
            if (string.IsNullOrWhiteSpace(pedido.Estado))
            {
                pedido.Estado = "Pendiente";
            }

            if (ModelState.IsValid)
            {
                var producto = await _context.Productos.FindAsync(pedido.ProductoId);

                if (producto == null)
                {
                    ModelState.AddModelError("", "El producto seleccionado no existe.");
                    CargarProductos(pedido.ProductoId);
                    return View(pedido);
                }

                if (pedido.Cantidad > producto.Stock)
                {
                    ModelState.AddModelError("", "No hay suficiente stock disponible para realizar el pedido.");
                    CargarProductos(pedido.ProductoId);
                    return View(pedido);
                }

                if (pedido.Estado == "Completado")
                {
                    producto.Stock -= pedido.Cantidad;

                    var movimiento = new MovimientoInventario
                    {
                        ProductoId = producto.Id,
                        TipoMovimiento = "Salida",
                        Cantidad = pedido.Cantidad,
                        FechaMovimiento = DateTime.Now,
                        Observacion = $"Salida por pedido del cliente {pedido.ClienteNombre}"
                    };

                    _context.MovimientosInventario.Add(movimiento);
                    _context.Productos.Update(producto);
                }

                pedido.Estado = "Pendiente";
                pedido.EstadoPago = "Pendiente";
                pedido.MetodoPago = "Sin definir";

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Pago),
                    new { id = pedido.Id });
            }

            CargarProductos(pedido.ProductoId);
            return View(pedido);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            if (User.IsInRole("Cliente") && !PedidoPerteneceAlCliente(pedido))
            {
                return Forbid();
            }

            return View(pedido);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null) return NotFound();

            CargarProductos(pedido.ProductoId);
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, Pedido pedido)
        {
            if (id != pedido.Id)
            {
                return NotFound();
            }

            var pedidoOriginal = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedidoOriginal == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Si el pedido pasa a Completado por primera vez, se descuenta stock.
                    if (pedidoOriginal.Estado != "Completado" && pedido.Estado == "Completado")
                    {
                        var producto = await _context.Productos.FindAsync(pedido.ProductoId);

                        if (producto == null)
                        {
                            ModelState.AddModelError("", "El producto seleccionado no existe.");
                            CargarProductos(pedido.ProductoId);
                            return View(pedido);
                        }

                        if (pedido.Cantidad > producto.Stock)
                        {
                            ModelState.AddModelError("", "No hay suficiente stock disponible para completar el pedido.");
                            CargarProductos(pedido.ProductoId);
                            return View(pedido);
                        }

                        producto.Stock -= pedido.Cantidad;

                        var movimiento = new MovimientoInventario
                        {
                            ProductoId = producto.Id,
                            TipoMovimiento = "Salida",
                            Cantidad = pedido.Cantidad,
                            FechaMovimiento = DateTime.Now,
                            Observacion = $"Salida por pedido completado del cliente {pedido.ClienteNombre}"
                        };

                        _context.MovimientosInventario.Add(movimiento);
                        _context.Productos.Update(producto);
                    }

                    pedido.ClienteCorreo = pedidoOriginal.ClienteCorreo;
                    pedido.MetodoPago = pedidoOriginal.MetodoPago;
                    pedido.EstadoPago = pedidoOriginal.EstadoPago;
                    _context.Update(pedido);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Pedido actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExists(pedido.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            CargarProductos(pedido.ProductoId);
            return View(pedido);
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> EditCliente(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            if (!PedidoPerteneceAlCliente(pedido))
            {
                return Forbid();
            }

            if (pedido.Estado != "Pendiente")
            {
                TempData["Error"] = "Solo puede modificar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            CargarProductos(pedido.ProductoId);
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> EditCliente(int id, Pedido pedido)
        {
            var pedidoOriginal = await _context.Pedidos.FindAsync(id);

            if (pedidoOriginal == null) return NotFound();

            if (!PedidoPerteneceAlCliente(pedidoOriginal))
            {
                return Forbid();
            }

            if (pedidoOriginal.Estado != "Pendiente")
            {
                TempData["Error"] = "Solo puede modificar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            var producto = await _context.Productos.FindAsync(pedido.ProductoId);

            if (producto == null)
            {
                ModelState.AddModelError("", "El producto seleccionado no existe.");
            }
            else if (pedido.Cantidad > producto.Stock)
            {
                ModelState.AddModelError("", "No hay suficiente stock disponible para actualizar el pedido.");
            }

            if (ModelState.IsValid)
            {
                pedidoOriginal.ProductoId = pedido.ProductoId;
                pedidoOriginal.Cantidad = pedido.Cantidad;
                pedidoOriginal.ClienteTelefono = pedido.ClienteTelefono;
                pedidoOriginal.Observacion = pedido.Observacion;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Pedido actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            pedido.ClienteNombre = pedidoOriginal.ClienteNombre;
            pedido.ClienteCorreo = pedidoOriginal.ClienteCorreo;
            pedido.ClienteTelefono = pedidoOriginal.ClienteTelefono;
            pedido.Estado = pedidoOriginal.Estado;
            pedido.FechaPedido = pedidoOriginal.FechaPedido;

            CargarProductos(pedido.ProductoId);
            return View(pedido);
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Cancelar(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            if (!PedidoPerteneceAlCliente(pedido))
            {
                return Forbid();
            }

            if (pedido.Estado != "Pendiente")
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
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null) return NotFound();

            if (!PedidoPerteneceAlCliente(pedido))
            {
                return Forbid();
            }

            if (pedido.Estado != "Pendiente")
            {
                TempData["Error"] = "Solo puede cancelar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            pedido.Estado = "Cancelado";
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
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido != null)
            {
                _context.Pedidos.Remove(pedido);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Pedido eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private void CargarProductos(int? productoSeleccionado = null)
        {
            ViewBag.ProductoId = new SelectList(
                _context.Productos
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Nombre),
                "Id",
                "Nombre",
                productoSeleccionado
            );
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
        }

        private bool PedidoPerteneceAlCliente(Pedido pedido)
        {
            return pedido.ClienteCorreo == User.Identity?.Name;
        }

        [Authorize(Roles = "Administrador,Vendedor")]
        public async Task<IActionResult> CambiarEstado(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Vendedor")]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            var estadoAnterior = pedido.Estado;

            if (string.IsNullOrWhiteSpace(estado))
            {
                ModelState.AddModelError("", "Debe seleccionar un estado.");
                return View(pedido);
            }

            if (estadoAnterior != "Completado" && estado == "Completado")
            {
                var producto = await _context.Productos.FindAsync(pedido.ProductoId);

                if (producto == null)
                {
                    ModelState.AddModelError("", "El producto seleccionado no existe.");
                    return View(pedido);
                }

                if (pedido.Cantidad > producto.Stock)
                {
                    ModelState.AddModelError("", "No hay suficiente stock disponible para completar el pedido.");
                    return View(pedido);
                }

                producto.Stock -= pedido.Cantidad;

                var movimiento = new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = "Salida",
                    Cantidad = pedido.Cantidad,
                    FechaMovimiento = DateTime.Now,
                    Observacion = $"Salida por pedido completado del cliente {pedido.ClienteNombre}"
                };

                _context.MovimientosInventario.Add(movimiento);
                _context.Productos.Update(producto);
            }

            pedido.Estado = estado;
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Pago(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
            {
                return NotFound();
            }

            ViewBag.Total = pedido.Producto?.Precio * pedido.Cantidad;
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pago(
         int pedidoId,
         string metodoPago,
         string? numeroTarjeta,
         string? fechaVencimiento,
         string? numeroSecreto)
        {
            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
            {
                return NotFound();
            }
            if (metodoPago == "Tarjeta")
            {
                var tarjetaLimpia = numeroTarjeta?.Replace(" ", "") ?? "";
                if (tarjetaLimpia.Length != 16)
                {
                    TempData["Error"] = "La tarjeta debe contener 16 dígitos.";
                    return RedirectToAction(nameof(Pago),
                        new { id = pedidoId });
                }
                if (string.IsNullOrWhiteSpace(fechaVencimiento))
                {
                    TempData["Error"] = "Debe ingresar la fecha de vencimiento.";
                    return RedirectToAction(nameof(Pago),
                        new { id = pedidoId });
                }
                if (string.IsNullOrWhiteSpace(numeroSecreto) ||
                    numeroSecreto.Length != 3)
                {
                    TempData["Error"] =
                        "El CVV debe contener 3 dígitos.";
                    return RedirectToAction(nameof(Pago),
                        new { id = pedidoId });
                }
            }
            Console.WriteLine($"Metodo: {metodoPago}");
            pedido.MetodoPago = metodoPago;
            pedido.EstadoPago = metodoPago == "Tarjeta"
                ? "Pagado"
                : "Pendiente de pago";

            await _context.SaveChangesAsync();
            TempData["Success"] =
                "Pago registrado correctamente.";
            return RedirectToAction(nameof(Details), new { id = pedido.Id });
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ConfirmarPago(int id)
        {
            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
            {
                return NotFound();
            }
            pedido.EstadoPago = "Pagado";
            await _context.SaveChangesAsync();
            TempData["Success"] = "Pago confirmado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}