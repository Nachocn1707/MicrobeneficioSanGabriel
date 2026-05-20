using System.ComponentModel.DataAnnotations;

namespace MicrobeneficioSanGabriel.Models
{
    public class RegistroFinanciero
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de registro es obligatorio.")]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; } = string.Empty; // Ingreso o Gasto

        [Required(ErrorMessage = "La descripción es obligatoria.")]
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
        public DateTime Fecha { get; set; } = DateTime.Today;

        [StringLength(100)]
        [Display(Name = "Categoría")]
        public string? Categoria { get; set; }

        [StringLength(250)]
        [Display(Name = "Observación")]
        public string? Observacion { get; set; }
    }
}