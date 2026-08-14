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
                var error = await AplicarSalidaAsync(pedido);
                if (error != null)
                {
                    return error;
                }
            }

            // Sprint 4: la venta contable nace del pedido completado y nunca de
            // una captura manual. Si el pedido deja de estar completado, se revierte
            // también el registro financiero automático dentro de la misma transacción.
            await SincronizarVentaFinancieraAsync(pedido);
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
                var error = await AplicarSalidaAsync(pedido);
                if (error != null)
                {
                    return error;
                }
            }

            await SincronizarVentaFinancieraAsync(pedido);
            return null;
        }

        public async Task<string?> PrepararEliminacionAsync(Pedido pedido)
        {
            if (pedido.InventarioAplicado)
            {
                var error = await RevertirSalidaAsync(pedido);
                if (error != null)
                {
                    return error;
                }
            }

            var ventasAutomaticas = await _context.RegistrosFinancieros
                .Where(r => r.EsAutomatico &&
                            r.OrigenTipo == OrigenMovimiento.Pedido &&
                            r.OrigenId == pedido.Id &&
                            r.Categoria == "Venta")
                .ToListAsync();

            if (ventasAutomaticas.Count > 0)
            {
                _context.RegistrosFinancieros.RemoveRange(ventasAutomaticas);
            }

            return null;
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
                FechaMovimiento = DateTime.UtcNow.AddHours(-6),
                Observacion = $"Salida automática por pedido #{pedido.Id} · Cliente: {pedido.ClienteNombre}",
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
                FechaMovimiento = DateTime.UtcNow.AddHours(-6),
                Observacion = $"Reversión automática del inventario del pedido #{pedido.Id} · Cliente: {pedido.ClienteNombre}",
                OrigenTipo = OrigenMovimiento.ReversionPedido,
                OrigenId = pedido.Id,
                EsAutomatico = true
            });

            return null;
        }

        private async Task SincronizarVentaFinancieraAsync(Pedido pedido)
        {
            if (pedido.Id <= 0)
            {
                return;
            }

            var registros = await _context.RegistrosFinancieros
                .Where(r => r.EsAutomatico &&
                            r.OrigenTipo == OrigenMovimiento.Pedido &&
                            r.OrigenId == pedido.Id &&
                            r.Categoria == "Venta")
                .OrderBy(r => r.Id)
                .ToListAsync();

            if (pedido.Estado != EstadosPedido.Completado)
            {
                if (registros.Count > 0)
                {
                    _context.RegistrosFinancieros.RemoveRange(registros);
                }
                return;
            }

            var venta = registros.FirstOrDefault();
            if (venta == null)
            {
                venta = new RegistroFinanciero
                {
                    Tipo = "Ingreso",
                    Categoria = "Venta",
                    Fecha = DateTime.UtcNow.AddHours(-6),
                    OrigenTipo = OrigenMovimiento.Pedido,
                    OrigenId = pedido.Id,
                    EsAutomatico = true
                };
                _context.RegistrosFinancieros.Add(venta);
            }

            venta.Descripcion = $"Venta automática del pedido PP-{pedido.Id:0000}";
            venta.Monto = pedido.TotalMostrar;
            venta.Destinatario = pedido.ClienteNombre;
            venta.ProductoNombre = pedido.ProductoNombreMostrar;
            venta.CantidadKg = pedido.Cantidad;
            venta.Observacion = "Generada automáticamente al completar el pedido. No requiere captura manual.";

            if (registros.Count > 1)
            {
                _context.RegistrosFinancieros.RemoveRange(registros.Skip(1));
            }
        }
    }
}
