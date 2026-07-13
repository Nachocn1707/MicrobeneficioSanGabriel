using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Services
{
    public interface IPedidoInventarioService
    {
        Task<string?> CambiarEstadoAsync(Pedido pedido, string nuevoEstado);
        Task<string?> ActualizarPedidoAsync(Pedido pedido, int productoId, decimal cantidad, string nuevoEstado);
        Task<string?> PrepararEliminacionAsync(Pedido pedido);
    }
}
