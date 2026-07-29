using MicrobeneficioSanGabriel.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class MovimientoInventario
    {
        public int Id { get; set; }

        // Nullable en base de datos para conservar el historial si se elimina el producto.
        // Debe permanecer nullable en el modelo de EF para que los movimientos
        // históricos puedan conservarse después de eliminar un producto.
        // Nullable en la base de datos para conservar movimientos históricos,
        // pero obligatorio en los formularios de creación y edición.
        [Required(ErrorMessage = "Debe seleccionar un producto")]
        [Display(Name = "Producto")]
        public int? ProductoId { get; set; }

        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }

        [StringLength(100)]
        public string? ProductoNombre { get; set; }

        [NotMapped]
        public string ProductoNombreMostrar =>
            Producto?.Nombre
            ?? (!string.IsNullOrWhiteSpace(ProductoNombre) ? ProductoNombre : "Producto eliminado");

        [Required(ErrorMessage = "El tipo de movimiento es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El tipo de movimiento es obligatorio")]
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
