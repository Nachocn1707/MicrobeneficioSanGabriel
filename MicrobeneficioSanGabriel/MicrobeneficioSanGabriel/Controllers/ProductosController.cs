using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Services;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador,Vendedor,Cliente")]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private static readonly string[] ExtensionesImagenPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long TamanoMaximoImagenBytes = 5 * 1024 * 1024;

        public ProductosController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View(new Producto
            {
                Activo = true,
                StockMinimo = 20,
                FechaRegistro = DateTime.Now
            });
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Categoria,Precio,Stock,StockMinimo,Descripcion,Activo,FechaRegistro")] Producto producto, IFormFile? imagenArchivo)
        {
            if (!ValidarImagenProducto(imagenArchivo))
            {
                return View(producto);
            }

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

                producto.ImagenUrl = await GuardarImagenProductoAsync(imagenArchivo);

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Categoria,Precio,Stock,StockMinimo,Descripcion,Activo,FechaRegistro")] Producto producto, IFormFile? imagenArchivo, bool eliminarImagenActual = false)
        {
            if (id != producto.Id)
            {
                return NotFound();
            }

            var productoDb = await _context.Productos.FindAsync(id);
            if (productoDb == null)
            {
                return NotFound();
            }

            if (!ValidarImagenProducto(imagenArchivo))
            {
                producto.ImagenUrl = productoDb.ImagenUrl;
                return View(producto);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    producto.Nombre = producto.Nombre?.Trim() ?? string.Empty;
                    producto.Categoria = producto.Categoria?.Trim() ?? string.Empty;

                    var duplicado = await _context.Productos.AnyAsync(p =>
                        p.Id != producto.Id && p.Nombre.ToLower() == producto.Nombre.ToLower());
                    if (duplicado)
                    {
                        ModelState.AddModelError(nameof(Producto.Nombre), "Ya existe otro producto con este nombre.");
                        producto.ImagenUrl = productoDb.ImagenUrl;
                        return View(producto);
                    }

                    productoDb.Nombre = producto.Nombre;
                    productoDb.Categoria = producto.Categoria;
                    productoDb.Precio = producto.Precio;
                    productoDb.Stock = producto.Stock;
                    productoDb.StockMinimo = producto.StockMinimo;
                    productoDb.Descripcion = producto.Descripcion;
                    productoDb.Activo = producto.Activo;
                    productoDb.FechaRegistro = producto.FechaRegistro;

                    if (imagenArchivo is { Length: > 0 })
                    {
                        EliminarImagenPersonalizada(productoDb.ImagenUrl);
                        productoDb.ImagenUrl = await GuardarImagenProductoAsync(imagenArchivo);
                    }
                    else if (eliminarImagenActual)
                    {
                        EliminarImagenPersonalizada(productoDb.ImagenUrl);
                        productoDb.ImagenUrl = null;
                    }

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

            producto.ImagenUrl = productoDb.ImagenUrl;
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

            EliminarImagenPersonalizada(producto.ImagenUrl);
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

        private bool ValidarImagenProducto(IFormFile? imagenArchivo)
        {
            if (imagenArchivo == null || imagenArchivo.Length == 0)
            {
                return true;
            }

            var extension = Path.GetExtension(imagenArchivo.FileName).ToLowerInvariant();
            if (!ExtensionesImagenPermitidas.Contains(extension))
            {
                ModelState.AddModelError("imagenArchivo", "La imagen debe ser JPG, JPEG, PNG o WEBP.");
                return false;
            }

            if (imagenArchivo.Length > TamanoMaximoImagenBytes)
            {
                ModelState.AddModelError("imagenArchivo", "La imagen no puede pesar más de 5 MB.");
                return false;
            }

            return true;
        }

        private async Task<string?> GuardarImagenProductoAsync(IFormFile? imagenArchivo)
        {
            if (imagenArchivo == null || imagenArchivo.Length == 0)
            {
                return null;
            }

            var extension = Path.GetExtension(imagenArchivo.FileName).ToLowerInvariant();
            var carpetaRelativa = Path.Combine("images", "products", "uploads");
            var carpetaFisica = Path.Combine(_env.WebRootPath, carpetaRelativa);
            Directory.CreateDirectory(carpetaFisica);

            var nombreArchivo = $"producto-{Guid.NewGuid():N}{extension}";
            var rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);

            await using var stream = new FileStream(rutaFisica, FileMode.Create);
            await imagenArchivo.CopyToAsync(stream);

            return $"/images/products/uploads/{nombreArchivo}";
        }

        private void EliminarImagenPersonalizada(string? imagenUrl)
        {
            if (string.IsNullOrWhiteSpace(imagenUrl) || !imagenUrl.StartsWith("/images/products/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var nombreArchivo = Path.GetFileName(imagenUrl);
            if (string.IsNullOrWhiteSpace(nombreArchivo))
            {
                return;
            }

            var rutaFisica = Path.Combine(_env.WebRootPath, "images", "products", "uploads", nombreArchivo);
            if (System.IO.File.Exists(rutaFisica))
            {
                System.IO.File.Delete(rutaFisica);
            }
        }
    }
}
