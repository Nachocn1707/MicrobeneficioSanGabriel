using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public async Task<IActionResult> Index(string? modulo, string? usuarioId, DateTime? fechaInicio, DateTime? fechaFin, int page = 1)
        {
            var query = _context.Auditorias.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(modulo))
            {
                query = query.Where(a => a.Modulo == modulo);
            }

            if (!string.IsNullOrWhiteSpace(usuarioId))
            {
                query = query.Where(a => a.UsuarioId == usuarioId);
            }

            if (fechaInicio.HasValue)
            {
                query = query.Where(a => a.Fecha >= fechaInicio.Value.Date);
            }

            if (fechaFin.HasValue)
            {
                var limite = fechaFin.Value.Date.AddDays(1);
                query = query.Where(a => a.Fecha < limite);
            }

            await CargarFiltrosAsync(modulo, usuarioId);

            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.ModuloSeleccionado = modulo;
            ViewBag.UsuarioIdSeleccionado = usuarioId;

            const int pageSize = 8;
            var totalRegistros = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRegistros / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRegistros = totalRegistros;
            ViewBag.TotalPages = totalPages;

            var registros = await query
                .OrderByDescending(a => a.Fecha)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Corrige visualmente nombres históricos duplicados (por ejemplo, "Administrador Administrador")
            // sin alterar el registro original de auditoría.
            foreach (var registro in registros)
            {
                registro.UsuarioNombre = NormalizarNombreDuplicado(registro.UsuarioNombre);
            }

            return View(registros);
        }

        private async Task CargarFiltrosAsync(string? moduloSeleccionado = null, string? usuarioIdSeleccionado = null)
        {
            var modulosBase = new List<string>
            {
                "Auditoría",
                "Contabilidad",
                "Facturas",
                "Fincas",
                "Inventario",
                "Lotes",
                "Pedidos",
                "Producción",
                "Productores",
                "Productos",
                "Reportes",
                "Trazabilidad",
                "Usuarios"
            };

            var modulosAuditoria = await _context.Auditorias
                .AsNoTracking()
                .Where(a => a.Modulo != null && a.Modulo != "")
                .Select(a => a.Modulo)
                .Distinct()
                .ToListAsync();

            ViewBag.Modulos = modulosBase
                .Union(modulosAuditoria)
                .OrderBy(m => m)
                .Select(m => new SelectListItem
                {
                    Value = m,
                    Text = m,
                    Selected = m == moduloSeleccionado
                })
                .ToList();

            var usuarios = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Nombre)
                .ThenBy(u => u.Apellidos)
                .ThenBy(u => u.Email)
                .Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellidos,
                    u.Email,
                    u.UserName
                })
                .ToListAsync();

            ViewBag.Usuarios = usuarios
                .Select(u =>
                {
                    var nombreCompleto = NormalizarNombreDuplicado($"{u.Nombre} {u.Apellidos}".Trim());
                    var identificador = u.Email ?? u.UserName ?? "Usuario";

                    var texto = string.IsNullOrWhiteSpace(nombreCompleto)
                        ? identificador
                        : nombreCompleto;

                    if (!string.IsNullOrWhiteSpace(identificador) &&
                        !string.Equals(texto, identificador, StringComparison.OrdinalIgnoreCase))
                    {
                        texto += $" — {identificador}";
                    }

                    return new SelectListItem
                    {
                        Value = u.Id,
                        Text = texto,
                        Selected = u.Id == usuarioIdSeleccionado
                    };
                })
                .ToList();
        }

        private static string NormalizarNombreDuplicado(string? valor)
        {
            var texto = (valor ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(texto))
            {
                return "Usuario";
            }

            var partes = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length >= 2 && partes.Length % 2 == 0)
            {
                var mitad = partes.Length / 2;
                var izquierda = string.Join(" ", partes.Take(mitad));
                var derecha = string.Join(" ", partes.Skip(mitad));

                if (string.Equals(izquierda, derecha, StringComparison.OrdinalIgnoreCase))
                {
                    return izquierda;
                }
            }

            return texto;
        }
    }
}