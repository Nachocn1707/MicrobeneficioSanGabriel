namespace MicrobeneficioSanGabriel.ViewModels
{
    /// <summary>
    /// Datos mínimos del pedido que se conservan temporalmente en la sesión.
    /// El pedido todavía no existe en la base de datos mientras esta clase se utiliza.
    /// </summary>
    public class PedidoPendienteSession
    {
        public string ClienteNombre { get; set; } = string.Empty;
        public string? ClienteId { get; set; }
        public string? ClienteCorreo { get; set; }
        public string ClienteTelefono { get; set; } = string.Empty;
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public string? Observacion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
