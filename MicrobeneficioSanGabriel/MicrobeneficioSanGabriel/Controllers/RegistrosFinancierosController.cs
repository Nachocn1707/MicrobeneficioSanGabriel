using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class RegistrosFinancierosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegistrosFinancierosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, string? tipo)
        {
            var registros = _context.RegistrosFinancieros.AsQueryable();

            if (fechaInicio.HasValue)
            {
                registros = registros.Where(r => r.Fecha >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                registros = registros.Where(r => r.Fecha < fechaFin.Value.Date.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                registros = registros.Where(r => r.Tipo == tipo);
            }

            var lista = await registros
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();

            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.Tipo = tipo;

            ViewBag.TotalIngresos = lista
                .Where(r => r.Tipo == "Ingreso")
                .Sum(r => r.Monto);

            ViewBag.TotalGastos = lista
                .Where(r => r.Tipo == "Gasto")
                .Sum(r => r.Monto);

            ViewBag.Balance = ViewBag.TotalIngresos - ViewBag.TotalGastos;

            return View(lista);
        }

        public async Task<IActionResult> ExportarExcel(DateTime? fechaInicio, DateTime? fechaFin, string? tipo)
        {
            var registros = _context.RegistrosFinancieros
                .AsNoTracking()
                .AsQueryable();

            if (fechaInicio.HasValue)
            {
                registros = registros.Where(r => r.Fecha >= fechaInicio.Value.Date);
            }

            if (fechaFin.HasValue)
            {
                var limite = fechaFin.Value.Date.AddDays(1);
                registros = registros.Where(r => r.Fecha < limite);
            }

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                registros = registros.Where(r => r.Tipo == tipo);
            }

            var lista = await registros.OrderByDescending(r => r.Fecha).ToListAsync();
            var totalIngresos = lista.Where(r => r.Tipo == "Ingreso").Sum(r => r.Monto);
            var totalGastos = lista.Where(r => r.Tipo == "Gasto").Sum(r => r.Monto);
            var balance = totalIngresos - totalGastos;

            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Finanzas");

            hoja.Cell("A1").Value = "REPORTE FINANCIERO";
            hoja.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(16);
            hoja.Cell("A2").Value = "Fecha de generación";
            hoja.Cell("B2").Value = DateTime.Now;
            hoja.Cell("B2").Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

            hoja.Cell("A4").Value = "Total ingresos";
            hoja.Cell("B4").Value = totalIngresos;
            hoja.Cell("A5").Value = "Total gastos";
            hoja.Cell("B5").Value = totalGastos;
            hoja.Cell("A6").Value = "Balance";
            hoja.Cell("B6").Value = balance;
            hoja.Range("B4:B6").Style.NumberFormat.Format = "₡ #,##0.00";

            var encabezados = new[] { "Fecha", "Tipo", "Categoría", "Descripción", "Monto", "Observación" };
            for (var columna = 0; columna < encabezados.Length; columna++)
            {
                hoja.Cell(8, columna + 1).Value = encabezados[columna];
            }
            hoja.Range("A8:F8").Style.Font.SetBold();

            var fila = 9;
            foreach (var item in lista)
            {
                hoja.Cell(fila, 1).Value = item.Fecha;
                hoja.Cell(fila, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                hoja.Cell(fila, 2).Value = item.Tipo;
                hoja.Cell(fila, 3).Value = item.Categoria;
                hoja.Cell(fila, 4).Value = ProtegerFormulaExcel(item.Descripcion);
                hoja.Cell(fila, 5).Value = item.Monto;
                hoja.Cell(fila, 5).Style.NumberFormat.Format = "₡ #,##0.00";
                hoja.Cell(fila, 6).Value = ProtegerFormulaExcel(item.Observacion);
                fila++;
            }

            hoja.Columns().AdjustToContents();
            hoja.SheetView.FreezeRows(8);
            hoja.RangeUsed()?.SetAutoFilter();

            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return File(
                memoria.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Reporte_Financiero_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        private static string ProtegerFormulaExcel(string? valor)
        {
            var texto = valor ?? string.Empty;
            return texto.Length > 0 && "=+-@".Contains(texto[0]) ? "'" + texto : texto;
        }

        public IActionResult Create()
        {
            return View(new RegistroFinanciero
            {
                Fecha = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegistroFinanciero registroFinanciero)
        {
            if (ModelState.IsValid)
            {
                _context.Add(registroFinanciero);
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Finanzas", "Crear", registroFinanciero.Id,
                    $"Se registró un {registroFinanciero.Tipo.ToLower()} por ₡{registroFinanciero.Monto:N2}.");

                TempData["Success"] = "Registro financiero creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            return View(registroFinanciero);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registro = await _context.RegistrosFinancieros.FindAsync(id);

            if (registro == null)
            {
                return NotFound();
            }

            return View(registro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RegistroFinanciero registroFinanciero)
        {
            if (id != registroFinanciero.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(registroFinanciero);
                    await _context.SaveChangesAsync();
                    await AuditoriaHelper.RegistrarAsync(
                        _context, User, "Finanzas", "Editar", registroFinanciero.Id,
                        $"Se actualizó el registro financiero #{registroFinanciero.Id}.");

                    TempData["Success"] = "Registro financiero actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RegistroFinancieroExists(registroFinanciero.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(registroFinanciero);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registro = await _context.RegistrosFinancieros
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registro == null)
            {
                return NotFound();
            }

            return View(registro);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registro = await _context.RegistrosFinancieros
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registro == null)
            {
                return NotFound();
            }

            return View(registro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registro = await _context.RegistrosFinancieros.FindAsync(id);

            if (registro != null)
            {
                _context.RegistrosFinancieros.Remove(registro);
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(
                    _context, User, "Finanzas", "Eliminar", id,
                    $"Se eliminó el registro financiero #{id}.");

                TempData["Success"] = "Registro financiero eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RegistroFinancieroExists(int id)
        {
            return _context.RegistrosFinancieros.Any(e => e.Id == id);
        }
    }
}