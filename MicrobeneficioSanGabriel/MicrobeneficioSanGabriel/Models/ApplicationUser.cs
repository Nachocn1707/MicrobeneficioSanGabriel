using Microsoft.AspNetCore.Identity;

namespace MicrobeneficioSanGabriel.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public virtual ICollection<MetodoPago> MetodosPago { get; set; } = new List<MetodoPago>();

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