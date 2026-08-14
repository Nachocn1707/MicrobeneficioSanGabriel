using MicrobeneficioSanGabriel.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class Lote
    {
        public int Id { get; set; }

        [Display(Name = "Código")]
        [StringLength(30)]
        public string? CodigoLote { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un productor")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un productor")]
        [Display(Name = "Productor")]
        public int? ProductorId { get; set; }

        [ForeignKey(nameof(ProductorId))]
        public Productor? Productor { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una finca")]
        [Display(Name = "Finca")]
        public int? FincaId { get; set; }

        [ForeignKey(nameof(FincaId))]
        public Finca? Finca { get; set; }

        [Required(ErrorMessage = "El peso es obligatorio")]
        [Range(1, 999999, ErrorMessage = "El peso debe ser mayor a 0")]
        [WholeNumber]
        [Display(Name = "Peso (kg)")]
        public decimal PesoKg { get; set; }

        [Required(ErrorMessage = "La fecha de recepción es obligatoria")]
        [Display(Name = "Fecha de recepción")]
        public DateTime FechaRecepcion { get; set; } = DateTime.UtcNow.AddHours(-6);

        [Required(ErrorMessage = "El estado es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El estado es obligatorio")]
        public string? Estado { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Observación")]
        public string? Observacion { get; set; }
    }
}
