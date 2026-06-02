using System.ComponentModel.DataAnnotations;

namespace MicrobeneficioSanGabriel.ViewModels
{
    public class PagoViewModel
    {
        public int PedidoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un método de pago.")]
        public string MetodoPago { get; set; } = string.Empty;

        [Display(Name = "Número de tarjeta")]
        [StringLength(19, ErrorMessage = "Número de tarjeta inválido.")]
        public string? NumeroTarjeta { get; set; }

        [Display(Name = "Fecha de vencimiento")]
        public string? FechaVencimiento { get; set; }

        [Display(Name = "CVV")]
        [StringLength(3, MinimumLength = 3,
            ErrorMessage = "El CVV debe contener 3 dígitos.")]
        public string? CVV { get; set; }
    }
}
