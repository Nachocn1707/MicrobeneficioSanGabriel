using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Productores/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Cedula,Telefono,Direccion,Finca,Activo,FechaRegistro")] Productor productor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(productor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(productor);
        }

        // GET: Productores/Edit/5
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(productor);
        }

        // GET: Productores/Delete/5
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
