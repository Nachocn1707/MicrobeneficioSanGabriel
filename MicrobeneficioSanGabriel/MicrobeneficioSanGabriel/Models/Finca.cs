using System.ComponentModel.DataAnnotations;
using MicrobeneficioSanGabriel.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class Finca
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la finca es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El nombre de la finca es obligatorio")]
        [StringLength(100, MinimumLength = 2)]
        public string? Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un productor")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un productor")]
        [Display(Name = "Productor")]
        public int? ProductorId { get; set; }

        [ForeignKey(nameof(ProductorId))]
        public Productor? Productor { get; set; }

        [Required(ErrorMessage = "Seleccione una provincia")]
        [NotWhiteSpace(ErrorMessage = "Seleccione una provincia")]
        [StringLength(50)]
        public string? Provincia { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cantón es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El cantón es obligatorio")]
        [StringLength(80)]
        [Display(Name = "Cantón")]
        public string? Canton { get; set; } = string.Empty;

        [Required(ErrorMessage = "El distrito es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El distrito es obligatorio")]
        [StringLength(80)]
        public string? Distrito { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección exacta es obligatoria")]
        [NotWhiteSpace(ErrorMessage = "La dirección exacta es obligatoria")]
        [StringLength(250, MinimumLength = 5)]
        [Display(Name = "Dirección exacta")]
        public string? DireccionExacta { get; set; } = string.Empty;

        public bool Activa { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public ICollection<Lote> Lotes { get; set; } = new List<Lote>();

        public string UbicacionCompleta =>
            string.Join(", ", new[] { Distrito, Canton, Provincia }
                .Where(valor => !string.IsNullOrWhiteSpace(valor)));
    }
}
