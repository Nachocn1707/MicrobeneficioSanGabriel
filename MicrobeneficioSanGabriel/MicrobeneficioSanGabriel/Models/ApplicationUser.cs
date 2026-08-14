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
                var nombre = Nombre?.Trim() ?? string.Empty;
                var apellidos = Apellidos?.Trim() ?? string.Empty;

                // Evita mostrar valores duplicados como "Administrador Administrador"
                // cuando bases históricas guardaron el mismo texto en ambos campos.
                if (!string.IsNullOrWhiteSpace(nombre) &&
                    string.Equals(nombre, apellidos, StringComparison.OrdinalIgnoreCase))
                {
                    return nombre;
                }

                var nombreCompleto = $"{nombre} {apellidos}".Trim();

                return string.IsNullOrWhiteSpace(nombreCompleto)
                    ? Email ?? UserName ?? "Usuario"
                    : nombreCompleto;
            }
        }
    }
}