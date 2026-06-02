using Microsoft.AspNetCore.Identity;

namespace MicrobeneficioSanGabriel.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string NombreCompleto
        {
            get
            {
                var nombreCompleto = $"{Nombre} {Apellidos}".Trim();

                return string.IsNullOrWhiteSpace(nombreCompleto)
                    ? Email ?? UserName ?? "Usuario"
                    : nombreCompleto;
            }
        }
    }
}