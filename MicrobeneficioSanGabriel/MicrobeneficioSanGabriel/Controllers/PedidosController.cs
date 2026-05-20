using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Vendedor")]
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
                .OrderByDescending(p => p.FechaPedido);

            return View(await pedidos.ToListAsync());
        }

        public IActionResult Create()
        {
            CargarProductos();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pedido pedido)
        {
            if (ModelState.IsValid)
            {
                var producto = await _context.Productos.FindAsync(pedido.ProductoId);

                if (producto == null)
                {
                    ModelState.AddModelError("", "El producto seleccionado no existe.");
                    CargarProductos(pedido.ProductoId);
                    return View(pedido);
                }

                if (pedido.Estado == "Completado")
                {
                    if (producto.Stock < pedido.Cantidad)
                    {
                        ModelState.AddModelError("", "No hay suficiente stock para completar el pedido.");
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
                        Observacion = $"Salida por pedido del cliente {pedido.ClienteNombre}"
                    };

                    _context.MovimientosInventario.Add(movimiento);
                    _context.Productos.Update(producto);
                }

                pedido.FechaPedido = DateTime.Now;

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

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

            return RedirectToAction(nameof(Index));
        }

        private void CargarProductos(int? productoSeleccionado = null)
        {
            ViewBag.ProductoId = new SelectList(
                _context.Productos.OrderBy(p => p.Nombre),
                "Id",
                "Nombre",
                productoSeleccionado
            );
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
        }
    }
}