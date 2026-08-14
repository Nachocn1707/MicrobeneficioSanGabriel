using System.ComponentModel.DataAnnotations;
using MicrobeneficioSanGabriel.Infrastructure;

namespace MicrobeneficioSanGabriel.Models
{
    public class RegistroFinanciero
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de registro es obligatorio.")]
        [NotWhiteSpace(ErrorMessage = "El tipo de registro es obligatorio.")]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; } = string.Empty; // Ingreso o Gasto

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [NotWhiteSpace(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(150)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Range(1, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [Display(Name = "Fecha")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.UtcNow.AddHours(-6).Date;

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [NotWhiteSpace(ErrorMessage = "La categoría es obligatoria.")]
        [StringLength(100)]
        [Display(Name = "Categoría")]
        public string? Categoria { get; set; }

        [StringLength(250)]
        [Display(Name = "Observación")]
        public string? Observacion { get; set; }

        [StringLength(40)]
        [Display(Name = "Origen")]
        public string? OrigenTipo { get; set; }

        [Display(Name = "Id de origen")]
        public int? OrigenId { get; set; }

        [Display(Name = "Registro automático")]
        public bool EsAutomatico { get; set; }

        [StringLength(180)]
        [Display(Name = "Destinatario / cliente")]
        public string? Destinatario { get; set; }

        [StringLength(120)]
        [Display(Name = "Producto")]
        public string? ProductoNombre { get; set; }

        [Display(Name = "Cantidad (kg)")]
        public decimal? CantidadKg { get; set; }
    }
}