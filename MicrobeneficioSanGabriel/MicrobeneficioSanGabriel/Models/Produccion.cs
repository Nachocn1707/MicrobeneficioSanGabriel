using MicrobeneficioSanGabriel.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class Produccion
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un lote")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un lote")]
        [Display(Name = "Lote")]
        public int LoteId { get; set; }

        [ForeignKey(nameof(LoteId))]
        public Lote? Lote { get; set; }

        [Required(ErrorMessage = "La fecha de producción es obligatoria")]
        [Display(Name = "Fecha de producción")]
        public DateTime FechaProduccion { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El tipo de proceso es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El tipo de proceso es obligatorio")]
        [Display(Name = "Tipo de proceso")]
        public string? TipoProceso { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cantidad procesada es obligatoria")]
        [Range(1, 999999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [WholeNumber]
        [Display(Name = "Cantidad procesada (Kg)")]
        public decimal CantidadProcesadaKg { get; set; }

        [Required(ErrorMessage = "La cantidad resultante es obligatoria")]
        [Range(1, 999999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [WholeNumber]
        [Display(Name = "Cantidad resultante (Kg)")]
        public decimal CantidadResultanteKg { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El estado es obligatorio")]
        public string? Estado { get; set; } = string.Empty;

        [Display(Name = "Observación")]
        public string? Observacion { get; set; }

        [Display(Name = "Producto resultante")]
        public int? ProductoId { get; set; }

        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }

        [StringLength(100)]
        public string? ProductoNombre { get; set; }

        [NotMapped]
        public string ProductoNombreMostrar =>
            Producto?.Nombre
            ?? (!string.IsNullOrWhiteSpace(ProductoNombre) ? ProductoNombre : "Sin producto resultante");
    }
}
