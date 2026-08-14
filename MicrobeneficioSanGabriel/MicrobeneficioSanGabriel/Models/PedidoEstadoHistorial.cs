using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    /// <summary>
    /// Conserva cada transición de estado de un pedido para que administración
    /// y cliente puedan consultar la trazabilidad completa con fecha y hora.
    /// </summary>
    public class PedidoEstadoHistorial
    {
        public int Id { get; set; }

        [Required]
        public int PedidoId { get; set; }

        [ForeignKey(nameof(PedidoId))]
        public Pedido? Pedido { get; set; }

        [StringLength(40)]
        public string? EstadoAnterior { get; set; }

        [Required]
        [StringLength(40)]
        public string EstadoNuevo { get; set; } = string.Empty;

        public DateTime FechaCambio { get; set; } = DateTime.UtcNow.AddHours(-6);

        [StringLength(450)]
        public string? UsuarioId { get; set; }

        [StringLength(180)]
        public string UsuarioNombre { get; set; } = "Sistema";

        [StringLength(80)]
        public string Rol { get; set; } = "Sistema";

        [StringLength(300)]
        public string? Observacion { get; set; }
    }
}
