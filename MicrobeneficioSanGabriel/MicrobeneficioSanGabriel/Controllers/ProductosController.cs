using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Services;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador,Vendedor,Cliente")]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Productos
        public async Task<IActionResult> Index()
        {
            return View(await _context.Productos.ToListAsync());
        }

        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // GET: Productos/Create
        [Authorize(Roles = "Administrador,Operador,Vendedor")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Operador,Vendedor")]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Categoria,Precio,Stock,StockMinimo,Descripcion,Activo,FechaRegistro")] Producto producto)
        {
            if (ModelState.IsValid)
            {
                producto.Nombre = producto.Nombre?.Trim() ?? string.Empty;
                producto.Categoria = producto.Categoria?.Trim() ?? string.Empty;
                producto.FechaRegistro = DateTime.Now;

                if (await _context.Productos.AnyAsync(p => p.Nombre.ToLower() == producto.Nombre.ToLower()))
                {
                    ModelState.AddModelError(nameof(Producto.Nombre), "Ya existe un producto con este nombre.");
                    return View(producto);
                }

                _context.Add(producto);
                await _context.SaveChangesAsync();
                await AuditoriaHelper.RegistrarAsync(_context, User, "Productos", "Crear", producto.Id,
                    $"Se registró el producto {producto.Nombre}.");
                TempData["Success"] = "Producto registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            return View(producto);
        }

        // GET: Productos/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Categoria,Precio,Stock,StockMinimo,Descripcion,Activo,FechaRegistro")] Producto producto)
        {
            if (id != producto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var duplicado = await _context.Productos.AnyAsync(p =>
                        p.Id != producto.Id && p.Nombre.ToLower() == producto.Nombre.ToLower());
                    if (duplicado)
                    {
                        ModelState.AddModelError(nameof(Producto.Nombre), "Ya existe otro producto con este nombre.");
                        return View(producto);
                    }

                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                    await AuditoriaHelper.RegistrarAsync(_context, User, "Productos", "Editar", producto.Id,
                        $"Se actualizó el producto {producto.Nombre}.");
                    TempData["Success"] = "Producto actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(producto);
        }

        // GET: Productos/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null) return NotFound();

            var tieneRelaciones = await _context.Pedidos.AnyAsync(p => p.ProductoId == id)
                || await _context.Producciones.AnyAsync(p => p.ProductoId == id)
                || await _context.MovimientosInventario.AnyAsync(m => m.ProductoId == id);

            if (tieneRelaciones)
            {
                TempData["Error"] = "No se puede eliminar el producto porque posee pedidos, producciones o movimientos de inventario asociados. Puede desactivarlo en su lugar.";
                return RedirectToAction(nameof(Index));
            }

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            await AuditoriaHelper.RegistrarAsync(_context, User, "Productos", "Eliminar", id,
                $"Se eliminó el producto {producto.Nombre}.");
            TempData["Success"] = "Producto eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}