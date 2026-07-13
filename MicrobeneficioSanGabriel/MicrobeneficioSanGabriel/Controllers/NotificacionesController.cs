using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize]
    [Route("Notificaciones")]
    public class NotificacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("Descartar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Descartar([FromBody] DescartarNotificacionRequest request)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var clave = request.Clave?.Trim();

            if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(clave) || clave.Length > 200)
            {
                return BadRequest(new { mensaje = "La alerta no es válida." });
            }

            var existe = await _context.NotificacionesUsuarios
                .AnyAsync(n => n.UsuarioId == usuarioId && n.Clave == clave);

            if (!existe)
            {
                _context.NotificacionesUsuarios.Add(new NotificacionUsuario
                {
                    UsuarioId = usuarioId,
                    Clave = clave,
                    FechaDescartada = DateTime.Now
                });
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Otro clic o pestaña pudo guardar la misma clave primero.
                    // El índice único garantiza que el resultado final siga siendo correcto.
                }
            }

            return Ok(new { descartada = true });
        }

        public sealed class DescartarNotificacionRequest
        {
            public string? Clave { get; set; }
        }
    }
}
