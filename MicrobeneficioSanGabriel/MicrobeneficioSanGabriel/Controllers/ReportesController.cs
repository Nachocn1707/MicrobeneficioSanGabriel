using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using ClosedXML.Excel;
using System.IO;

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

            ViewBag.Productos = await _context.Productos
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View();
        }

        public IActionResult ExportarExcel()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte");

                worksheet.Cell(1, 1).Value = "Microbeneficio San Gabriel";
                worksheet.Cell(2, 1).Value = "Reporte general administrativo";
                worksheet.Cell(3, 1).Value = $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}";

                worksheet.Cell(5, 1).Value = "Indicador";
                worksheet.Cell(5, 2).Value = "Valor";

                worksheet.Cell(6, 1).Value = "Productores";
                worksheet.Cell(6, 2).Value = _context.Productores.Count();

                worksheet.Cell(7, 1).Value = "Productos";
                worksheet.Cell(7, 2).Value = _context.Productos.Count();

                worksheet.Cell(8, 1).Value = "Pedidos";
                worksheet.Cell(8, 2).Value = _context.Pedidos.Count();

                worksheet.Cell(9, 1).Value = "Facturas";
                worksheet.Cell(9, 2).Value = _context.Facturas.Count();

                worksheet.Cell(10, 1).Value = "Ventas totales";
                worksheet.Cell(10, 2).Value = _context.Facturas.Sum(f => f.Total);

                worksheet.Cell(11, 1).Value = "Stock total disponible";
                worksheet.Cell(11, 2).Value = _context.Productos.Sum(p => p.Stock);

                worksheet.Cell(12, 1).Value = "Productos con stock bajo";
                worksheet.Cell(12, 2).Value = _context.Productos.Count(p => p.Stock <= p.StockMinimo);

                worksheet.Range("A1:B1").Merge();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 18;

                worksheet.Range("A5:B5").Style.Font.Bold = true;
                worksheet.Range("A5:B5").Style.Fill.BackgroundColor = XLColor.DarkBlue;
                worksheet.Range("A5:B5").Style.Font.FontColor = XLColor.White;

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