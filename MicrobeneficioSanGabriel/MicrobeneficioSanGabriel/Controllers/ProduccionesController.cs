using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador")]
    public class ProduccionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProduccionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Producciones
        public async Task<IActionResult> Index()
        {
            var producciones = _context.Producciones
                .Include(p => p.Lote)
                .Include(p => p.Producto)
                .OrderByDescending(p => p.FechaProduccion);

            return View(await producciones.ToListAsync());
        }

        // GET: Producciones/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produccion = await _context.Producciones
                .Include(p => p.Lote)
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (produccion == null)
            {
                return NotFound();
            }

            return View(produccion);
        }

        // GET: Producciones/Create
        public IActionResult Create()
        {
            CargarCombos();
            return View();
        }

        // POST: Producciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Produccion produccion)
        {
            if (ModelState.IsValid)
            {
                produccion.FechaProduccion = DateTime.Now;

                _context.Producciones.Add(produccion);
                await _context.SaveChangesAsync();

                // Solo aumenta stock si la producción nace como Completado.
                if (produccion.Estado == "Completado" && produccion.ProductoId.HasValue)
                {
                    var producto = await _context.Productos.FindAsync(produccion.ProductoId.Value);

                    if (producto != null)
                    {
                        producto.Stock += (int)produccion.CantidadResultanteKg;

                        var movimiento = new MovimientoInventario
                        {
                            ProductoId = producto.Id,
                            TipoMovimiento = "Entrada",
                            Cantidad = (int)produccion.CantidadResultanteKg,
                            FechaMovimiento = DateTime.Now,
                            Observacion = $"Entrada automática por producción #{produccion.Id}"
                        };

                        _context.MovimientosInventario.Add(movimiento);
                        _context.Productos.Update(producto);

                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            CargarCombos(produccion.LoteId, produccion.ProductoId);
            return View(produccion);
        }

        // GET: Producciones/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produccion = await _context.Producciones.FindAsync(id);

            if (produccion == null)
            {
                return NotFound();
            }

            CargarCombos(produccion.LoteId, produccion.ProductoId);

            return View(produccion);
        }

        // POST: Producciones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Produccion produccion)
        {
            if (id != produccion.Id)
            {
                return NotFound();
            }

            var produccionOriginal = await _context.Producciones
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produccionOriginal == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Si la producción pasa a Completado por primera vez, se suma stock.
                    if (produccionOriginal.Estado != "Completado" &&
                        produccion.Estado == "Completado" &&
                        produccion.ProductoId.HasValue)
                    {
                        var producto = await _context.Productos.FindAsync(produccion.ProductoId.Value);

                        if (producto != null)
                        {
                            producto.Stock += (int)produccion.CantidadResultanteKg;

                            var movimiento = new MovimientoInventario
                            {
                                ProductoId = producto.Id,
                                TipoMovimiento = "Entrada",
                                Cantidad = (int)produccion.CantidadResultanteKg,
                                FechaMovimiento = DateTime.Now,
                                Observacion = $"Entrada automática por producción #{produccion.Id}"
                            };

                            _context.MovimientosInventario.Add(movimiento);
                            _context.Productos.Update(producto);
                        }
                    }

                    _context.Update(produccion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProduccionExists(produccion.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            CargarCombos(produccion.LoteId, produccion.ProductoId);
            return View(produccion);
        }

        // GET: Producciones/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produccion = await _context.Producciones
                .Include(p => p.Lote)
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (produccion == null)
            {
                return NotFound();
            }

            return View(produccion);
        }

        // POST: Producciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var produccion = await _context.Producciones.FindAsync(id);

            if (produccion != null)
            {
                _context.Producciones.Remove(produccion);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private void CargarCombos(int? loteId = null, int? productoId = null)
        {
            ViewBag.LoteId = new SelectList(
                _context.Lotes.OrderBy(l => l.CodigoLote),
                "Id",
                "CodigoLote",
                loteId
            );

            ViewBag.ProductoId = new SelectList(
                _context.Productos.OrderBy(p => p.Nombre),
                "Id",
                "Nombre",
                productoId
            );
        }

        private bool ProduccionExists(int id)
        {
            return _context.Producciones.Any(e => e.Id == id);
        }
    }
}