using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class NotificacionUsuario
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UsuarioId { get; set; } = string.Empty;

        [ForeignKey(nameof(UsuarioId))]
        public ApplicationUser? Usuario { get; set; }

        [Required]
        [StringLength(200)]
        public string Clave { get; set; } = string.Empty;

        public DateTime FechaDescartada { get; set; } = DateTime.Now;
    }
}
