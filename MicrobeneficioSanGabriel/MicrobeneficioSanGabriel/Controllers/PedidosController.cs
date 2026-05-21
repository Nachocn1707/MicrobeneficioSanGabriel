using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Vendedor,Cliente")]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Producto)
                .OrderByDescending(p => p.FechaPedido)
                .AsQueryable();

            if (User.IsInRole("Cliente"))
            {
                var correoCliente = User.Identity?.Name;

                pedidos = pedidos.Where(p =>
                    p.ClienteCorreo == correoCliente ||
                    (p.ClienteCorreo == null && p.ClienteNombre == correoCliente)
                );
            }

            return View(await pedidos.ToListAsync());
        }

        public IActionResult Create()
        {
            CargarProductos();

            var pedido = new Pedido
            {
                FechaPedido = DateTime.Now,
                Estado = "Pendiente"
            };

            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pedido pedido)
        {
            if (User.IsInRole("Cliente"))
            {
                pedido.ClienteCorreo = User.Identity?.Name;
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

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Pedido creado correctamente.";
                return RedirectToAction(nameof(Index));
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
            if (id != pedido.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pedido);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Pedido actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExists(pedido.Id)) return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
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
            var correoCliente = User.Identity?.Name;

            return pedido.ClienteCorreo == correoCliente ||
                   (pedido.ClienteCorreo == null && pedido.ClienteNombre == correoCliente);
        }
    }
}