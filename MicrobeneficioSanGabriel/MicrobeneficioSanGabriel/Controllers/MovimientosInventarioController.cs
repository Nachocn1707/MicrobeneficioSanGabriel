using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador")]
    public class MovimientosInventarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovimientosInventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var movimientos = _context.MovimientosInventario
                .Include(m => m.Producto)
                .OrderByDescending(m => m.FechaMovimiento);

            return View(await movimientos.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var movimientoInventario = await _context.MovimientosInventario
                .Include(m => m.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movimientoInventario == null) return NotFound();

            return View(movimientoInventario);
        }

        public IActionResult Create()
        {
            ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProductoId,TipoMovimiento,Cantidad,Observacion,FechaMovimiento")] MovimientoInventario movimientoInventario)
        {
            if (ModelState.IsValid)
            {
                var producto = await _context.Productos.FindAsync(movimientoInventario.ProductoId);

                if (producto == null)
                {
                    ModelState.AddModelError("", "El producto seleccionado no existe.");
                    ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre", movimientoInventario.ProductoId);
                    return View(movimientoInventario);
                }

                if (movimientoInventario.TipoMovimiento == "Entrada")
                {
                    producto.Stock += movimientoInventario.Cantidad;
                }
                else if (movimientoInventario.TipoMovimiento == "Salida")
                {
                    if (producto.Stock < movimientoInventario.Cantidad)
                    {
                        ModelState.AddModelError("", "No hay suficiente stock para realizar la salida.");
                        ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre", movimientoInventario.ProductoId);
                        return View(movimientoInventario);
                    }

                    producto.Stock -= movimientoInventario.Cantidad;
                }
                else
                {
                    ModelState.AddModelError("", "Debe seleccionar un tipo de movimiento válido.");
                    ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre", movimientoInventario.ProductoId);
                    return View(movimientoInventario);
                }

                movimientoInventario.FechaMovimiento = DateTime.Now;

                _context.Add(movimientoInventario);
                _context.Update(producto);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre", movimientoInventario.ProductoId);
            return View(movimientoInventario);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var movimientoInventario = await _context.MovimientosInventario.FindAsync(id);

            if (movimientoInventario == null) return NotFound();

            ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre", movimientoInventario.ProductoId);
            return View(movimientoInventario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductoId,TipoMovimiento,Cantidad,Observacion,FechaMovimiento")] MovimientoInventario movimientoInventario)
        {
            if (id != movimientoInventario.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movimientoInventario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovimientoInventarioExists(movimientoInventario.Id))
                        return NotFound();

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductoId"] = new SelectList(_context.Productos, "Id", "Nombre", movimientoInventario.ProductoId);
            return View(movimientoInventario);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var movimientoInventario = await _context.MovimientosInventario
                .Include(m => m.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movimientoInventario == null) return NotFound();

            return View(movimientoInventario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movimientoInventario = await _context.MovimientosInventario.FindAsync(id);

            if (movimientoInventario != null)
            {
                _context.MovimientosInventario.Remove(movimientoInventario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovimientoInventarioExists(int id)
        {
            return _context.MovimientosInventario.Any(e => e.Id == id);
        }
    }
}