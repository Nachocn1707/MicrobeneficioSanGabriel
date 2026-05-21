using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using Microsoft.AspNetCore.Authorization;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador,Operador")]
    public class ProductoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Productores
        public async Task<IActionResult> Index()
        {
            return View(await _context.Productores.ToListAsync());
        }

        // GET: Productores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productor = await _context.Productores
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productor == null)
            {
                return NotFound();
            }

            return View(productor);
        }

        // GET: Productores/Create
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Productores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Categoria,Precio,Stock,Descripcion,Activo,FechaRegistro")] Producto producto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(producto);
        }

        // GET: Productores/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productor = await _context.Productores.FindAsync(id);

            if (productor == null)
            {
                return NotFound();
            }

            return View(productor);
        }

        // POST: Productores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Cedula,Telefono,Direccion,Finca,Activo,FechaRegistro")] Productor productor)
        {
            if (id != productor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductorExists(productor.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(productor);
        }

        // GET: Productores/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productor = await _context.Productores
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productor == null)
            {
                return NotFound();
            }

            return View(productor);
        }

        // POST: Productores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productor = await _context.Productores.FindAsync(id);

            if (productor != null)
            {
                _context.Productores.Remove(productor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductorExists(int id)
        {
            return _context.Productores.Any(e => e.Id == id);
        }
    }
}