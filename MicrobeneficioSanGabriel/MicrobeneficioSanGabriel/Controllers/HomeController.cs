using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;


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

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            if (User.IsInRole("Cliente"))
            {
                return RedirectToAction(nameof(ClienteDashboard));
            }

            if (!(User.IsInRole("Administrador") || User.IsInRole("Operador") || User.IsInRole("Vendedor")))
            {
                return RedirectToAction(nameof(AccessDenied));
            }

            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);
            var inicioAnio = new DateTime(hoy.Year, 1, 1);
            var finAnio = inicioAnio.AddYears(1);

            ViewBag.PeriodoActual = hoy.ToString("MMMM yyyy", new CultureInfo("es-CR"));
            ViewBag.AnioActual = hoy.Year;

            ViewBag.TotalUsuarios = await _userManager.Users.CountAsync();
            ViewBag.TotalProductores = await _context.Productores.CountAsync();
            ViewBag.TotalProductos = await _context.Productos.CountAsync();
            ViewBag.TotalInventario = await _context.MovimientosInventario.CountAsync();
            ViewBag.TotalPedidos = await _context.Pedidos.CountAsync();
            ViewBag.TotalFacturas = await _context.Facturas.CountAsync();
            ViewBag.TotalFincas = await _context.Fincas.CountAsync(f => f.Activa);
            ViewBag.TotalLotes = await _context.Lotes.CountAsync();
            ViewBag.ProductoresActivos = await _context.Productores.CountAsync(p => p.Activo);
            ViewBag.StockBajo = await _context.Productos.CountAsync(p => p.Stock <= p.StockMinimo);

            ViewBag.VentasMes = await _context.Facturas
                .Where(f => f.FechaFactura >= inicioMes && f.FechaFactura < finMes &&
                            f.EstadoPago != "Anulada" && f.EstadoPago != "Cancelado")
                .SumAsync(f => (decimal?)f.Total) ?? 0m;

            ViewBag.TotalVentas = await _context.Facturas
                .Where(f => f.EstadoPago != "Anulada" && f.EstadoPago != "Cancelado")
                .SumAsync(f => (decimal?)f.Total) ?? 0m;

            ViewBag.ProduccionMesKg = await _context.Producciones
                .Where(p => p.FechaProduccion >= inicioMes && p.FechaProduccion < finMes)
                .SumAsync(p => (double?)p.CantidadResultanteKg) ?? 0d;

            ViewBag.LotesActivos = await _context.Lotes
                .CountAsync(l => l.Estado != "Finalizado" && l.Estado != "Cancelado");

            ViewBag.PedidosPendientes = await _context.Pedidos
                .CountAsync(p => p.Estado == "Pendiente" || p.Estado == "En proceso");

            ViewBag.PedidosCompletados = await _context.Pedidos
                .CountAsync(p => p.Estado == "Completado");

            ViewBag.TotalKgRecibidos = await _context.Lotes
                .SumAsync(l => (double?)l.PesoKg) ?? 0d;

            ViewBag.ProduccionesEnProceso = await _context.Producciones
                .CountAsync(p => p.Estado == "En proceso");

            ViewBag.ProduccionesCompletadasHoy = await _context.Producciones
                .CountAsync(p => p.Estado == "Completado" && p.FechaProduccion.Date == hoy);

            ViewBag.TotalStockKg = await _context.Productos
                .Where(p => p.Activo)
                .SumAsync(p => (int?)p.Stock) ?? 0;

            var ventasPorMes = await _context.Facturas
                .Where(f => f.FechaFactura >= inicioAnio && f.FechaFactura < finAnio &&
                            f.EstadoPago != "Anulada" && f.EstadoPago != "Cancelado")
                .GroupBy(f => f.FechaFactura.Month)
                .Select(g => new { Mes = g.Key, Total = g.Sum(f => f.Total) })
                .ToListAsync();

            var ventasMensuales = Enumerable.Range(1, 12)
                .Select(m => ventasPorMes.FirstOrDefault(v => v.Mes == m)?.Total ?? 0m)
                .ToList();
            ViewBag.VentasMensuales = ventasMensuales;

            ViewBag.EstadosPedidos = await _context.Pedidos
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Estado) ? "Sin estado" : p.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Estado, x => x.Cantidad);

            ViewBag.ProductosInventario = await _context.Productos
                .Where(p => p.Activo)
                .OrderByDescending(p => p.Stock)
                .ThenBy(p => p.Nombre)
                .Take(5)
                .ToListAsync();

            ViewBag.ProcesosPorEstado = await _context.Producciones
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Estado) ? "Sin estado" : p.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Estado, x => x.Cantidad);

            ViewBag.TotalRegistrosTrazabilidad = await _context.Trazabilidades.CountAsync();
            ViewBag.LotesConTrazabilidad = await _context.Trazabilidades
                .Select(t => t.LoteId)
                .Distinct()
                .CountAsync();

            var totalLotes = await _context.Lotes.CountAsync();
            var lotesConTrazabilidad = Convert.ToInt32(ViewBag.LotesConTrazabilidad ?? 0);
            ViewBag.PorcentajeTrazabilidad = totalLotes == 0
                ? 0
                : Math.Round((decimal)lotesConTrazabilidad * 100m / totalLotes, 0);

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
            if (User.IsInRole("Cliente"))
            {
                return RedirectToAction(nameof(ClienteDashboard));
            }

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