using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Services;
using ClosedXML.Excel;
using System.IO;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Vendedor")]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReporteIAService _reporteIAService;

        public ReportesController(
            ApplicationDbContext context,
            IReporteIAService reporteIAService)
        {
            _context = context;
            _reporteIAService = reporteIAService;
        }

        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var facturasQuery = _context.Facturas.AsQueryable();
            var pedidosQuery = _context.Pedidos.AsQueryable();

            if (fechaInicio.HasValue)
            {
                facturasQuery = facturasQuery.Where(f => f.FechaFactura >= fechaInicio.Value);
                pedidosQuery = pedidosQuery.Where(p => p.FechaPedido >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                var fechaFinCompleta = fechaFin.Value.Date.AddDays(1).AddTicks(-1);

                facturasQuery = facturasQuery.Where(f => f.FechaFactura <= fechaFinCompleta);
                pedidosQuery = pedidosQuery.Where(p => p.FechaPedido <= fechaFinCompleta);
            }

            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            ViewBag.TotalProductores = await _context.Productores.CountAsync();
            ViewBag.TotalProductos = await _context.Productos.CountAsync();

            ViewBag.TotalPedidos = await pedidosQuery.CountAsync();
            ViewBag.TotalFacturas = await facturasQuery.CountAsync();

            ViewBag.TotalVentas = await facturasQuery
                .Where(f => f.EstadoPago != "Anulada" && f.EstadoPago != "Cancelado")
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            ViewBag.TotalStock = await _context.Productos
                .SumAsync(p => (decimal?)p.Stock) ?? 0;

            ViewBag.PedidosPendientes = await pedidosQuery
                .CountAsync(p => p.Estado == "Pendiente" || p.Estado == "En proceso");

            ViewBag.FacturasPendientes = await facturasQuery
                .CountAsync(f => f.EstadoPago == "Pendiente");

            ViewBag.ProductosStockBajo = await _context.Productos
                .CountAsync(p => p.Stock <= p.StockMinimo);

            ViewBag.Productos = await _context.Productos
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View();
        }

        public async Task<IActionResult> AnalisisIA(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var analisis = await _reporteIAService.GenerarAnalisisAsync(fechaInicio, fechaFin);
            return View(analisis);
        }

        public IActionResult ExportarExcel(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var facturasQuery = _context.Facturas.AsQueryable();
            var pedidosQuery = _context.Pedidos.AsQueryable();

            if (fechaInicio.HasValue)
            {
                facturasQuery = facturasQuery.Where(f => f.FechaFactura >= fechaInicio.Value);
                pedidosQuery = pedidosQuery.Where(p => p.FechaPedido >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                var fechaFinCompleta = fechaFin.Value.Date.AddDays(1).AddTicks(-1);

                facturasQuery = facturasQuery.Where(f => f.FechaFactura <= fechaFinCompleta);
                pedidosQuery = pedidosQuery.Where(p => p.FechaPedido <= fechaFinCompleta);
            }

            var totalPedidos = pedidosQuery.Count();
            var totalFacturas = facturasQuery.Count();

            var totalVentas = facturasQuery
                .Where(f => f.EstadoPago != "Anulada" && f.EstadoPago != "Cancelado")
                .Sum(f => (decimal?)f.Total) ?? 0;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte");

                worksheet.Cell(1, 1).Value = "Microbeneficio San Gabriel";
                worksheet.Cell(2, 1).Value = "Reporte general administrativo";

                worksheet.Cell(3, 1).Value =
                    fechaInicio.HasValue || fechaFin.HasValue
                        ? $"Periodo: {fechaInicio?.ToString("dd/MM/yyyy") ?? "Inicio"} - {fechaFin?.ToString("dd/MM/yyyy") ?? "Actual"}"
                        : "Periodo: General";

                worksheet.Cell(4, 1).Value = $"Fecha de generacion: {DateTime.Now:dd/MM/yyyy HH:mm}";

                worksheet.Cell(6, 1).Value = "Indicador";
                worksheet.Cell(6, 2).Value = "Valor";

                worksheet.Cell(7, 1).Value = "Productores";
                worksheet.Cell(7, 2).Value = _context.Productores.Count();

                worksheet.Cell(8, 1).Value = "Productos";
                worksheet.Cell(8, 2).Value = _context.Productos.Count();

                worksheet.Cell(9, 1).Value = "Pedidos";
                worksheet.Cell(9, 2).Value = totalPedidos;

                worksheet.Cell(10, 1).Value = "Facturas";
                worksheet.Cell(10, 2).Value = totalFacturas;

                worksheet.Cell(11, 1).Value = "Ventas totales";
                worksheet.Cell(11, 2).Value = totalVentas;

                worksheet.Cell(12, 1).Value = "Stock total disponible (kg)";
                worksheet.Cell(12, 2).Value = _context.Productos.Sum(p => (decimal?)p.Stock) ?? 0;

                worksheet.Cell(13, 1).Value = "Productos con stock bajo";
                worksheet.Cell(13, 2).Value = _context.Productos.Count(p => p.Stock <= p.StockMinimo);

                worksheet.Range("A1:B1").Merge();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 18;

                worksheet.Range("A6:B6").Style.Font.Bold = true;
                worksheet.Range("A6:B6").Style.Fill.BackgroundColor = XLColor.DarkBlue;
                worksheet.Range("A6:B6").Style.Font.FontColor = XLColor.White;

                worksheet.Cell(11, 2).Style.NumberFormat.Format = "CRC #,##0.00";
                worksheet.Cell(12, 2).Style.NumberFormat.Format = "#,##0 \"kg\"";

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "ReporteMicrobeneficio.xlsx"
                    );
                }
            }
        }
    }
}