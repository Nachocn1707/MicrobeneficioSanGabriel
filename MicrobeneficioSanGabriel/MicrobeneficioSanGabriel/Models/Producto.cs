using System.ComponentModel.DataAnnotations;

namespace MicrobeneficioSanGabriel.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        [StringLength(50)]
        public string Categoria { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 9999999, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio")]
        [Range(0, 999999, ErrorMessage = "El stock no puede ser negativo")]
        public decimal Stock { get; set; }

        [Display(Name = "Stock mínimo")]
        public decimal StockMinimo { get; set; } = 20m;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        [Display(Name = "Imagen del producto")]
        [StringLength(250)]
        public string? ImagenUrl { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}