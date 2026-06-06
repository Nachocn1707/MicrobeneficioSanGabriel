using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class Lote
    {
        public int Id { get; set; }

        [Display(Name = "Código")]
        public string CodigoLote { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un productor")]
        [Display(Name = "Productor")]
        public int ProductorId { get; set; }

        [ForeignKey("ProductorId")]
        public Productor? Productor { get; set; }

        [Required(ErrorMessage = "El peso es obligatorio")]
        [Range(0.01, 999999, ErrorMessage = "El peso debe ser mayor a 0")]
        [Display(Name = "Peso (Kg)")]
        public double PesoKg { get; set; }

        [Required(ErrorMessage = "La fecha de recepción es obligatoria")]
        [Display(Name = "Fecha Recepción")]
        public DateTime FechaRecepcion { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El estado es obligatorio")]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = string.Empty;

        [Display(Name = "Observación")]
        public string? Observacion { get; set; }
    }
}