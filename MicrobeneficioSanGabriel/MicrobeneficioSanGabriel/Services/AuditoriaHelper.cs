using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Services
{
    public static class AuditoriaHelper
    {
        public static async Task RegistrarAsync(
            ApplicationDbContext context,
            ClaimsPrincipal usuario,
            string modulo,
            string accion,
            int? registroId = null,
            string? detalle = null)
        {
            try
            {
                var usuarioId = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
                ApplicationUser? applicationUser = null;

                if (!string.IsNullOrWhiteSpace(usuarioId))
                {
                    applicationUser = await context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == usuarioId);
                }

                var rol = usuario.FindFirstValue(ClaimTypes.Role)
                    ?? usuario.Claims.FirstOrDefault(c => c.Type.EndsWith("/role"))?.Value
                    ?? "Usuario";

                var nombre = applicationUser?.NombreCompleto
                    ?? usuario.Identity?.Name
                    ?? "Usuario";

                context.Auditorias.Add(new AuditoriaRegistro
                {
                    UsuarioId = usuarioId,
                    UsuarioNombre = nombre,
                    Rol = rol,
                    Modulo = modulo,
                    Accion = accion,
                    RegistroId = registroId,
                    Detalle = detalle,
                    Fecha = DateTime.Now
                });

                await context.SaveChangesAsync();
            }
            catch
            {
                // La auditoría nunca debe interrumpir la operación principal.
            }
        }
    }
}
