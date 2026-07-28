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

            var productoNuevo = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == productoId);

            if (productoNuevo == null)
            {
                return "El producto seleccionado no existe.";
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
            pedido.ProductoNombre = productoNuevo.Nombre;
            pedido.PrecioUnitario = productoNuevo.Precio;
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
            if (!pedido.ProductoId.HasValue)
            {
                return "El producto de este pedido fue eliminado. Seleccione otro producto antes de completarlo.";
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == pedido.ProductoId.Value);

            if (producto == null)
            {
                return "El producto seleccionado ya no existe. Seleccione otro producto antes de completar el pedido.";
            }

            if (pedido.Cantidad <= 0)
            {
                return "La cantidad del pedido debe ser mayor a cero.";
            }

            if (producto.Stock < pedido.Cantidad)
            {
                return $"No hay suficiente stock. Disponible: {producto.Stock:N2} kg.";
            }

            pedido.ProductoNombre = producto.Nombre;
            if (pedido.PrecioUnitario <= 0)
            {
                pedido.PrecioUnitario = producto.Precio;
            }

            producto.Stock -= pedido.Cantidad;
            pedido.InventarioAplicado = true;

            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = producto.Id,
                ProductoNombre = producto.Nombre,
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
            // Si el producto ya fue eliminado, no existe inventario que devolver.
            // El pedido y su historial sí pueden continuar gestionándose.
            if (!pedido.ProductoId.HasValue)
            {
                pedido.InventarioAplicado = false;
                return null;
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == pedido.ProductoId.Value);

            if (producto == null)
            {
                pedido.ProductoId = null;
                pedido.InventarioAplicado = false;
                return null;
            }

            producto.Stock += pedido.Cantidad;
            pedido.InventarioAplicado = false;

            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = producto.Id,
                ProductoNombre = producto.Nombre,
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
