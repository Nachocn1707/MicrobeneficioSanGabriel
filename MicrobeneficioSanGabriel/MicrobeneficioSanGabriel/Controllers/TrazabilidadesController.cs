using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Controllers
{
    public class TrazabilidadesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrazabilidadesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Trazabilidades
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Trazabilidades.Include(t => t.Lote).Include(t => t.Produccion);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Trazabilidades/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trazabilidad = await _context.Trazabilidades
                .Include(t => t.Lote)
                .Include(t => t.Produccion)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trazabilidad == null)
            {
                return NotFound();
            }

            return View(trazabilidad);
        }

        // GET: Trazabilidades/Create
        public IActionResult Create()
        {
            ViewData["LoteId"] = new SelectList(_context.Lotes, "Id", "CodigoLote");
            ViewData["ProduccionId"] = new SelectList(_context.Producciones, "Id", "Estado");
            return View();
        }

        // POST: Trazabilidades/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,LoteId,ProduccionId,Etapa,FechaRegistro,Responsable,Observacion")] Trazabilidad trazabilidad)
        {
            if (ModelState.IsValid)
            {
                _context.Add(trazabilidad);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LoteId"] = new SelectList(_context.Lotes, "Id", "CodigoLote", trazabilidad.LoteId);
            ViewData["ProduccionId"] = new SelectList(_context.Producciones, "Id", "Estado", trazabilidad.ProduccionId);
            return View(trazabilidad);
        }

        // GET: Trazabilidades/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trazabilidad = await _context.Trazabilidades.FindAsync(id);
            if (trazabilidad == null)
            {
                return NotFound();
            }
            ViewData["LoteId"] = new SelectList(_context.Lotes, "Id", "CodigoLote", trazabilidad.LoteId);
            ViewData["ProduccionId"] = new SelectList(_context.Producciones, "Id", "Estado", trazabilidad.ProduccionId);
            return View(trazabilidad);
        }

        // POST: Trazabilidades/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LoteId,ProduccionId,Etapa,FechaRegistro,Responsable,Observacion")] Trazabilidad trazabilidad)
        {
            if (id != trazabilidad.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trazabilidad);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrazabilidadExists(trazabilidad.Id))
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
            ViewData["LoteId"] = new SelectList(_context.Lotes, "Id", "CodigoLote", trazabilidad.LoteId);
            ViewData["ProduccionId"] = new SelectList(_context.Producciones, "Id", "Estado", trazabilidad.ProduccionId);
            return View(trazabilidad);
        }

        // GET: Trazabilidades/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trazabilidad = await _context.Trazabilidades
                .Include(t => t.Lote)
                .Include(t => t.Produccion)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trazabilidad == null)
            {
                return NotFound();
            }

            return View(trazabilidad);
        }

        // POST: Trazabilidades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trazabilidad = await _context.Trazabilidades.FindAsync(id);
            if (trazabilidad != null)
            {
                _context.Trazabilidades.Remove(trazabilidad);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TrazabilidadExists(int id)
        {
            return _context.Trazabilidades.Any(e => e.Id == id);
        }
    }
}
