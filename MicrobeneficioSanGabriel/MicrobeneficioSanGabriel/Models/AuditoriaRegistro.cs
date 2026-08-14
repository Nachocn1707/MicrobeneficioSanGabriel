using System.ComponentModel.DataAnnotations;

namespace MicrobeneficioSanGabriel.Models
{
    public class AuditoriaRegistro
    {
        public int Id { get; set; }

        [StringLength(450)]
        public string? UsuarioId { get; set; }

        [Required]
        [StringLength(180)]
        public string UsuarioNombre { get; set; } = "Usuario";

        [Required]
        [StringLength(80)]
        public string Rol { get; set; } = "Usuario";

        [Required]
        [StringLength(80)]
        public string Modulo { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string Accion { get; set; } = string.Empty;

        public int? RegistroId { get; set; }

        [StringLength(500)]
        public string? Detalle { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow.AddHours(-6);
    }
}
