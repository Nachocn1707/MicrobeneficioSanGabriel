using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MicrobeneficioSanGabriel.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsuarios = await _userManager.Users.CountAsync();
            ViewBag.TotalProductores = await _context.Productores.CountAsync();
            ViewBag.TotalProductos = await _context.Productos.CountAsync();
            ViewBag.TotalInventario = await _context.MovimientosInventario.CountAsync();

            ViewBag.TotalPedidos = await _context.Pedidos.CountAsync();
            ViewBag.TotalFacturas = await _context.Facturas.CountAsync();
            ViewBag.TotalVentas = await _context.Facturas.SumAsync(f => f.Total);
            ViewBag.StockBajo = await _context.Productos.CountAsync(p => p.Stock <= p.StockMinimo);

            ViewBag.UltimosPedidos = await _context.Pedidos
                .Include(p => p.Producto)
                .OrderByDescending(p => p.FechaPedido)
                .Take(5)
                .ToListAsync();

            ViewBag.UltimasFacturas = await _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p.Producto)
                .OrderByDescending(f => f.FechaFactura)
                .Take(5)
                .ToListAsync();

            ViewBag.ProductosStockBajo = await _context.Productos
                .Where(p => p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock)
                .Take(5)
                .ToListAsync();

            ViewBag.NombresProductos = await _context.Productos
                .OrderBy(p => p.Nombre)
                .Select(p => p.Nombre)
                .ToListAsync();

            ViewBag.StockProductos = await _context.Productos
                .OrderBy(p => p.Nombre)
                .Select(p => p.Stock)
                .ToListAsync();

            ViewBag.VentasProductos = await _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p.Producto)
                .Where(f => f.Pedido != null && f.Pedido.Producto != null)
                .GroupBy(f => f.Pedido!.Producto!.Nombre)
                .Select(g => new
                {
                    Producto = g.Key,
                    Total = g.Sum(x => x.Total)
                })
                .ToListAsync();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}