using System.ComponentModel.DataAnnotations;
using MicrobeneficioSanGabriel.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class Factura
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un pedido")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un pedido")]
        [Display(Name = "Pedido")]
        public int PedidoId { get; set; }

        [ForeignKey("PedidoId")]
        public Pedido? Pedido { get; set; }

        [Required]
        [Display(Name = "Fecha de factura")]
        public DateTime FechaFactura { get; set; } = DateTime.UtcNow.AddHours(-6);

        [Required]
        [Display(Name = "Subtotal")]
        public decimal Subtotal { get; set; }

        [Required]
        [Display(Name = "IVA")]
        public decimal IVA { get; set; }

        [Required]
        [Display(Name = "Total")]
        public decimal Total { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El estado es obligatorio")]
        public string EstadoPago { get; set; } = "Pendiente";

        [Display(Name = "Observación")]
        public string? Observacion { get; set; }
    }
}