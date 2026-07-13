using MicrobeneficioSanGabriel.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class MovimientoInventario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un producto")]
        public int ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto? Producto { get; set; }

        [Required(ErrorMessage = "El tipo de movimiento es obligatorio")]
        [StringLength(20)]
        public string TipoMovimiento { get; set; } = string.Empty; // Entrada o Salida

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, 999999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [WholeNumber]
        public decimal Cantidad { get; set; }

        [StringLength(300)]
        public string? Observacion { get; set; }

        public DateTime FechaMovimiento { get; set; } = DateTime.Now;

        [StringLength(40)]
        public string? OrigenTipo { get; set; }

        public int? OrigenId { get; set; }

        public bool EsAutomatico { get; set; }

    }
}