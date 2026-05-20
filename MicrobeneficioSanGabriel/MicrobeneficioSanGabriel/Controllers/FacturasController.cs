using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using Rotativa.AspNetCore;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Vendedor")]
    public class FacturasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FacturasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var facturas = _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p.Producto)
                .OrderByDescending(f => f.FechaFactura);

            return View(await facturas.ToListAsync());
        }

        public IActionResult Create()
        {
            CargarPedidos();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Factura factura)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == factura.PedidoId);

            if (pedido == null)
            {
                ModelState.AddModelError("", "El pedido seleccionado no existe.");
            }

            if (ModelState.IsValid)
            {
                factura.FechaFactura = DateTime.Now;
                factura.Subtotal = pedido!.Cantidad * pedido.Producto!.Precio;
                factura.IVA = factura.Subtotal * 0.13m;
                factura.Total = factura.Subtotal + factura.IVA;

                _context.Facturas.Add(factura);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            CargarPedidos(factura.PedidoId);
            return View(factura);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            return View(factura);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas.FindAsync(id);

            if (factura == null) return NotFound();

            CargarPedidos(factura.PedidoId);
            return View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, Factura factura)
        {
            if (id != factura.Id) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == factura.PedidoId);

            if (pedido == null)
            {
                ModelState.AddModelError("", "El pedido seleccionado no existe.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    factura.Subtotal = pedido!.Cantidad * pedido.Producto!.Precio;
                    factura.IVA = factura.Subtotal * 0.13m;
                    factura.Total = factura.Subtotal + factura.IVA;

                    _context.Update(factura);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FacturaExists(factura.Id)) return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            CargarPedidos(factura.PedidoId);
            return View(factura);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            return View(factura);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var factura = await _context.Facturas.FindAsync(id);

            if (factura != null)
            {
                _context.Facturas.Remove(factura);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Imprimir(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var factura = await _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p.Producto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null)
            {
                return NotFound();
            }

            return new ViewAsPdf("FacturaPDF", factura)
            {
                FileName = $"Factura_{factura.Id}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4
            };
        }

        private void CargarPedidos(int? pedidoSeleccionado = null)
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Producto)
                .OrderByDescending(p => p.FechaPedido)
                .ToList()
                .Select(p => new
                {
                    Id = p.Id,
                    Nombre = $"{p.ClienteNombre} - {p.Producto!.Nombre} - {p.Cantidad} unidades"
                });

            ViewBag.PedidoId = new SelectList(
                pedidos,
                "Id",
                "Nombre",
                pedidoSeleccionado
            );
        }

        private bool FacturaExists(int id)
        {
            return _context.Facturas.Any(e => e.Id == id);
        }
    }
}