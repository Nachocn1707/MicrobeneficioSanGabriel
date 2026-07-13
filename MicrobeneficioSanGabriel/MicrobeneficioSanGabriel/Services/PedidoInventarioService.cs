using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Constants;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Services
{
    public class PedidoInventarioService : IPedidoInventarioService
    {
        private readonly ApplicationDbContext _context;

        public PedidoInventarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string?> CambiarEstadoAsync(Pedido pedido, string nuevoEstado)
        {
            if (!EstadosPedido.EsValido(nuevoEstado))
            {
                return "El estado seleccionado no es válido.";
            }

            if (pedido.Estado == nuevoEstado)
            {
                return null;
            }

            if (pedido.InventarioAplicado && nuevoEstado != EstadosPedido.Completado)
            {
                var error = await RevertirSalidaAsync(pedido);
                if (error != null)
                {
                    return error;
                }
            }

            pedido.Estado = nuevoEstado;

            if (!pedido.InventarioAplicado && nuevoEstado == EstadosPedido.Completado)
            {
                return await AplicarSalidaAsync(pedido);
            }

            return null;
        }

        public async Task<string?> ActualizarPedidoAsync(
            Pedido pedido,
            int productoId,
            decimal cantidad,
            string nuevoEstado)
        {
            if (cantidad <= 0)
            {
                return "La cantidad debe ser mayor a cero.";
            }

            if (!EstadosPedido.EsValido(nuevoEstado))
            {
                return "El estado seleccionado no es válido.";
            }

            var cambiaDatosInventario = pedido.ProductoId != productoId || pedido.Cantidad != cantidad;

            if (pedido.InventarioAplicado && (cambiaDatosInventario || nuevoEstado != EstadosPedido.Completado))
            {
                var error = await RevertirSalidaAsync(pedido);
                if (error != null)
                {
                    return error;
                }
            }

            pedido.ProductoId = productoId;
            pedido.Cantidad = cantidad;
            pedido.Estado = nuevoEstado;

            if (nuevoEstado == EstadosPedido.Completado && !pedido.InventarioAplicado)
            {
                return await AplicarSalidaAsync(pedido);
            }

            return null;
        }

        public async Task<string?> PrepararEliminacionAsync(Pedido pedido)
        {
            if (!pedido.InventarioAplicado)
            {
                return null;
            }

            return await RevertirSalidaAsync(pedido);
        }

        private async Task<string?> AplicarSalidaAsync(Pedido pedido)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == pedido.ProductoId);

            if (producto == null)
            {
                return "El producto seleccionado no existe.";
            }

            if (pedido.Cantidad <= 0)
            {
                return "La cantidad del pedido debe ser mayor a cero.";
            }

            if (producto.Stock < pedido.Cantidad)
            {
                return $"No hay suficiente stock. Disponible: {producto.Stock:N2} kg.";
            }

            producto.Stock -= pedido.Cantidad;
            pedido.InventarioAplicado = true;

            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = producto.Id,
                TipoMovimiento = "Salida",
                Cantidad = pedido.Cantidad,
                FechaMovimiento = DateTime.Now,
                Observacion = $"Salida automática por pedido #{pedido.Id}",
                OrigenTipo = OrigenMovimiento.Pedido,
                OrigenId = pedido.Id,
                EsAutomatico = true
            });

            return null;
        }

        private async Task<string?> RevertirSalidaAsync(Pedido pedido)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == pedido.ProductoId);

            if (producto == null)
            {
                return "No se encontró el producto del pedido para devolver el inventario.";
            }

            producto.Stock += pedido.Cantidad;
            pedido.InventarioAplicado = false;

            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = producto.Id,
                TipoMovimiento = "Entrada",
                Cantidad = pedido.Cantidad,
                FechaMovimiento = DateTime.Now,
                Observacion = $"Reversión automática del inventario del pedido #{pedido.Id}",
                OrigenTipo = OrigenMovimiento.ReversionPedido,
                OrigenId = pedido.Id,
                EsAutomatico = true
            });

            return null;
        }
    }
}
