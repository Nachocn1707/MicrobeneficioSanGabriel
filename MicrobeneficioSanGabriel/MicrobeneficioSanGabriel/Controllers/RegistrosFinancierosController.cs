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
                registros = registros.Where(r => r.Fecha <= fechaFin.Value);
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
            var registros = _context.RegistrosFinancieros.AsQueryable();

            if (fechaInicio.HasValue)
            {
                registros = registros.Where(r => r.Fecha >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                registros = registros.Where(r => r.Fecha <= fechaFin.Value);
            }

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                registros = registros.Where(r => r.Tipo == tipo);
            }

            var lista = await registros
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();

            var totalIngresos = lista
                .Where(r => r.Tipo == "Ingreso")
                .Sum(r => r.Monto);

            var totalGastos = lista
                .Where(r => r.Tipo == "Gasto")
                .Sum(r => r.Monto);

            var balance = totalIngresos - totalGastos;

            var csv = new System.Text.StringBuilder();

            csv.AppendLine("REPORTE FINANCIERO");
            csv.AppendLine($"Fecha de generación;{DateTime.Now:dd/MM/yyyy}");
            csv.AppendLine();

            csv.AppendLine($"Total ingresos;₡ {totalIngresos:N2}");
            csv.AppendLine($"Total gastos;₡ {totalGastos:N2}");
            csv.AppendLine($"Balance;₡ {balance:N2}");
            csv.AppendLine();

            csv.AppendLine("Fecha;Tipo;Categoría;Descripción;Monto;Observación");

            foreach (var item in lista)
            {
                csv.AppendLine(
                    $"{item.Fecha:dd/MM/yyyy};" +
                    $"{item.Tipo};" +
                    $"{item.Categoria};" +
                    $"{item.Descripcion};" +
                    $"₡ {item.Monto:N2};" +
                    $"{item.Observacion}"
                );
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
                .ToArray();

            var nombreArchivo = $"Reporte_Financiero_{DateTime.Now:yyyyMMdd_HHmm}.csv";

            return File(bytes, "text/csv", nombreArchivo);
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