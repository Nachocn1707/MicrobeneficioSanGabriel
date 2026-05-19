using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalProductores = await _context.Productores.CountAsync();
            ViewBag.TotalProductos = await _context.Productos.CountAsync();
            ViewBag.TotalPedidos = await _context.Pedidos.CountAsync();
            ViewBag.TotalFacturas = await _context.Facturas.CountAsync();

            ViewBag.TotalVentas = await _context.Facturas.SumAsync(f => f.Total);
            ViewBag.TotalStock = await _context.Productos.SumAsync(p => p.Stock);

            ViewBag.PedidosPendientes = await _context.Pedidos.CountAsync(p => p.Estado == "Pendiente");
            ViewBag.FacturasPendientes = await _context.Facturas.CountAsync(f => f.EstadoPago == "Pendiente");

            ViewBag.ProductosStockBajo = await _context.Productos.CountAsync(p => p.Stock <= p.StockMinimo);

            return View();
        }
    }
}