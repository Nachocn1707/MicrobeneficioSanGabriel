using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AuditoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? modulo, string? usuario, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.Auditorias.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(modulo))
                query = query.Where(a => a.Modulo.Contains(modulo));

            if (!string.IsNullOrWhiteSpace(usuario))
                query = query.Where(a => a.UsuarioNombre.Contains(usuario));

            if (fechaInicio.HasValue)
                query = query.Where(a => a.Fecha >= fechaInicio.Value.Date);

            if (fechaFin.HasValue)
            {
                var limite = fechaFin.Value.Date.AddDays(1);
                query = query.Where(a => a.Fecha < limite);
            }

            ViewBag.Modulo = modulo;
            ViewBag.Usuario = usuario;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            return View(await query.OrderByDescending(a => a.Fecha).Take(500).ToListAsync());
        }
    }
}
