using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly IAnalisisInventarioIAService _analisisInventarioIAService;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger,
            IAnalisisInventarioIAService analisisInventarioIAService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _analisisInventarioIAService = analisisInventarioIAService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Administrador") || User.IsInRole("Operador") || User.IsInRole("Vendedor"))
                {
                    return RedirectToAction(nameof(Dashboard));
                }

                if (User.IsInRole("Cliente"))
                {
                    return RedirectToAction(nameof(ClienteDashboard));
                }

                return RedirectToAction(nameof(AccessDenied));
            }

            ViewBag.ProductosPublicos = await _context.Productos
                .Where(p => p.Activo)
                .OrderByDescending(p => p.Stock)
                .ThenBy(p => p.Nombre)
                .Take(6)
                .ToListAsync();

            ViewBag.TotalProductoresPublico = await _context.Productores.CountAsync(p => p.Activo);
            ViewBag.TotalFincasPublico = await _context.Fincas.CountAsync(f => f.Activa);
            ViewBag.TotalLotesPublico = await _context.Lotes.CountAsync();

            return View();
        }

        [Authorize(Roles = "Administrador,Operador,Vendedor")]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsuarios = await _userManager.Users.CountAsync();
            ViewBag.TotalProductores = await _context.Productores.CountAsync();
            ViewBag.TotalProductos = await _context.Productos.CountAsync();
            ViewBag.TotalInventario = await _context.MovimientosInventario.CountAsync();

            ViewBag.TotalPedidos = await _context.Pedidos.CountAsync();
            ViewBag.TotalFacturas = await _context.Facturas.CountAsync();
            ViewBag.TotalVentas = await _context.Facturas
                .Where(f => f.EstadoPago != "Anulada")
                .SumAsync(f => (decimal?)f.Total) ?? 0m;
            ViewBag.StockBajo = await _context.Productos.CountAsync(p => p.Stock <= p.StockMinimo);

            ViewBag.TotalFincas = await _context.Fincas.CountAsync(f => f.Activa);
            ViewBag.TotalLotes = await _context.Lotes.CountAsync();
            ViewBag.LotesActivos = await _context.Lotes.CountAsync(l => l.Estado != "Finalizado" && l.Estado != "Cancelado");
            ViewBag.TotalKgRecibidos = await _context.Lotes.SumAsync(l => (double?)l.PesoKg) ?? 0d;
            ViewBag.ProduccionesEnProceso = await _context.Producciones.CountAsync(p => p.Estado == "En proceso");
            ViewBag.ProductoresActivos = await _context.Productores.CountAsync(p => p.Activo);
            ViewBag.TotalStockKg = await _context.Productos.SumAsync(p => (int?)p.Stock) ?? 0;

            ViewBag.UltimosPedidos = await _context.Pedidos
                .Include(p => p.Producto)
                .OrderByDescending(p => p.FechaPedido)
                .Take(5)
                .ToListAsync();

            ViewBag.UltimosLotes = await _context.Lotes
                .Include(l => l.Productor)
                .Include(l => l.Finca)
                .OrderByDescending(l => l.FechaRecepcion)
                .Take(6)
                .ToListAsync();

            ViewBag.ProcesosPorTipo = await _context.Producciones
                .GroupBy(p => p.TipoProceso)
                .Select(g => new
                {
                    Tipo = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
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

            ViewBag.AlertasInventarioIA = await _analisisInventarioIAService.GenerarAlertasAsync();

            return View();
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> ClienteDashboard()
        {
            var usuarioActual = await _userManager.GetUserAsync(User);
            var correo = usuarioActual?.Email ?? User.Identity?.Name ?? string.Empty;

            var pedidosClienteQuery = _context.Pedidos
                .Include(p => p.Producto)
                .Where(p => !string.IsNullOrWhiteSpace(p.ClienteCorreo) && p.ClienteCorreo == correo);

            ViewBag.ClienteNombre = usuarioActual?.NombreCompleto ?? "Cliente";
            ViewBag.TotalProductos = await _context.Productos.CountAsync(p => p.Activo);
            ViewBag.TotalPedidosCliente = await pedidosClienteQuery.CountAsync();
            ViewBag.PedidosPendientesCliente = await pedidosClienteQuery.CountAsync(p => p.Estado != "Completado");
            ViewBag.PedidosCompletadosCliente = await pedidosClienteQuery.CountAsync(p => p.Estado == "Completado");
            ViewBag.UltimosPedidosCliente = await pedidosClienteQuery
                .OrderByDescending(p => p.FechaPedido)
                .Take(5)
                .ToListAsync();

            ViewBag.ProductosDestacados = await _context.Productos
                .Where(p => p.Activo)
                .OrderByDescending(p => p.Stock)
                .ThenBy(p => p.Nombre)
                .Take(3)
                .ToListAsync();

            ViewBag.TotalKgDisponibles = await _context.Productos
                .Where(p => p.Activo)
                .SumAsync(p => (int?)p.Stock) ?? 0;

            return View();
        }

        public IActionResult AccessDenied()
        {
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