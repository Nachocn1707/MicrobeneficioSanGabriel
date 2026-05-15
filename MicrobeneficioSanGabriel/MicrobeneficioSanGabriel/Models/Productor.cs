using System.ComponentModel.DataAnnotations;

namespace MicrobeneficioSanGabriel.Models
{
    public class Productor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [StringLength(20)]
        public string Cedula { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(20)]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [StringLength(200)]
        public string Direccion { get; set; }

        [StringLength(100)]
        public string? Finca { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}