using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Services
{
    public static class PedidoHistorialHelper
    {
        public static async Task RegistrarAsync(
            ApplicationDbContext context,
            ClaimsPrincipal usuario,
            Pedido pedido,
            string? estadoAnterior,
            string estadoNuevo,
            string? observacion = null)
        {
            if (pedido.Id <= 0 || string.IsNullOrWhiteSpace(estadoNuevo))
            {
                return;
            }

            // Evita duplicar la misma transición si el navegador reenvía una petición.
            var ultimo = await context.PedidoEstadoHistoriales
                .AsNoTracking()
                .Where(h => h.PedidoId == pedido.Id)
                .OrderByDescending(h => h.FechaCambio)
                .ThenByDescending(h => h.Id)
                .FirstOrDefaultAsync();

            if (ultimo != null &&
                string.Equals(ultimo.EstadoNuevo, estadoNuevo, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ultimo.EstadoAnterior ?? string.Empty, estadoAnterior ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

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
                ?? "Sistema";

            var nombre = applicationUser?.NombreCompleto
                ?? usuario.Identity?.Name
                ?? "Sistema";

            context.PedidoEstadoHistoriales.Add(new PedidoEstadoHistorial
            {
                PedidoId = pedido.Id,
                EstadoAnterior = string.IsNullOrWhiteSpace(estadoAnterior) ? null : estadoAnterior,
                EstadoNuevo = estadoNuevo,
                FechaCambio = DateTime.UtcNow.AddHours(-6),
                UsuarioId = usuarioId,
                UsuarioNombre = nombre,
                Rol = rol,
                Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim()
            });
        }
    }
}
