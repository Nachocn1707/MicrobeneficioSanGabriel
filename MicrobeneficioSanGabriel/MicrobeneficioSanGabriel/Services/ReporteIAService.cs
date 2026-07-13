using MicrobeneficioSanGabriel.Constants;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MicrobeneficioSanGabriel.Services
{
    public class ReporteIAService : IReporteIAService
    {
        private readonly ApplicationDbContext _context;

        public ReporteIAService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReporteIAViewModel> GenerarAnalisisAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var inicio = fechaInicio?.Date;
            var fin = fechaFin?.Date.AddDays(1);

            var pedidosQuery = _context.Pedidos
                .Include(p => p.Producto)
                .AsQueryable();

            var facturasQuery = _context.Facturas
                .Include(f => f.Pedido)
                .ThenInclude(p => p!.Producto)
                .AsQueryable();

            if (inicio.HasValue)
            {
                pedidosQuery = pedidosQuery.Where(p => p.FechaPedido >= inicio.Value);
                facturasQuery = facturasQuery.Where(f => f.FechaFactura >= inicio.Value);
            }

            if (fin.HasValue)
            {
                pedidosQuery = pedidosQuery.Where(p => p.FechaPedido < fin.Value);
                facturasQuery = facturasQuery.Where(f => f.FechaFactura < fin.Value);
            }

            var pedidos = await pedidosQuery.ToListAsync();
            var facturas = await facturasQuery.ToListAsync();
            var productos = await _context.Productos.ToListAsync();

            var facturasValidas = facturas
                .Where(f => f.EstadoPago == EstadosPago.PagoCompletado)
                .ToList();

            var modelo = new ReporteIAViewModel
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                VentasTotales = facturasValidas.Sum(f => f.Total),
                TotalPedidos = pedidos.Count,
                TotalFacturas = facturas.Count,
                PedidosPendientes = pedidos.Count(p => p.Estado == "Pendiente" || p.Estado == "En proceso"),
                PedidosCompletados = pedidos.Count(p => p.Estado == "Completado"),
                FacturasPendientes = facturas.Count(f => EstadosPago.EsPendiente(f.EstadoPago)),
                ProductosStockBajo = productos.Count(p => p.Stock <= p.StockMinimo),
                StockTotal = productos.Sum(p => p.Stock)
            };

            modelo.ProductosMasVendidos = pedidos
                .Where(p => p.Producto != null && p.Estado == EstadosPedido.Completado)
                .GroupBy(p => p.Producto!.Nombre)
                .Select(g => new ProductoVendidoIAItem
                {
                    Producto = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad),
                    TotalVentas = facturasValidas
                        .Where(f => f.Pedido?.Producto?.Nombre == g.Key)
                        .Sum(f => f.Total)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToList();

            modelo.ClientesFrecuentes = pedidos
                .GroupBy(p => string.IsNullOrWhiteSpace(p.ClienteNombre) ? "Sin cliente" : p.ClienteNombre)
                .Select(g => new ClienteFrecuenteIAItem
                {
                    Cliente = g.Key,
                    Pedidos = g.Count(),
                    TotalComprado = facturasValidas
                        .Where(f => f.Pedido != null && f.Pedido.ClienteNombre == g.Key)
                        .Sum(f => f.Total)
                })
                .OrderByDescending(x => x.Pedidos)
                .Take(5)
                .ToList();

            modelo.EstadosPedidos = pedidos
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Estado) ? "Sin estado" : p.Estado)
                .Select(g => new EstadoPedidoIAItem
                {
                    Estado = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .ToList();

            modelo.ResumenEjecutivo = GenerarResumen(modelo);
            modelo.Recomendaciones = GenerarRecomendaciones(modelo);

            return modelo;
        }

        private static string GenerarResumen(ReporteIAViewModel modelo)
        {
            if (modelo.TotalPedidos == 0 && modelo.TotalFacturas == 0)
            {
                return "No hay suficientes datos para generar un análisis operativo completo. Se recomienda registrar pedidos, facturas y movimientos para obtener mejores conclusiones.";
            }

            var productoPrincipal = modelo.ProductosMasVendidos.FirstOrDefault()?.Producto ?? "sin producto dominante";
            var clientePrincipal = modelo.ClientesFrecuentes.FirstOrDefault()?.Cliente ?? "sin cliente frecuente";

            return $"El sistema registra {modelo.TotalPedidos} pedido(s), {modelo.TotalFacturas} factura(s) y ventas por ₡ {modelo.VentasTotales:N2}. " +
                   $"El producto con mayor movimiento es {productoPrincipal}. El cliente más frecuente es {clientePrincipal}.";
        }

        private static List<RecomendacionIAItem> GenerarRecomendaciones(ReporteIAViewModel modelo)
        {
            var recomendaciones = new List<RecomendacionIAItem>();

            if (modelo.ProductosStockBajo > 0)
            {
                recomendaciones.Add(new RecomendacionIAItem
                {
                    Tipo = "warning",
                    Icono = "fa-boxes-stacked",
                    Titulo = "Revisar inventario",
                    Mensaje = $"Hay {modelo.ProductosStockBajo} producto(s) con stock bajo. Se recomienda reabastecer antes de aceptar pedidos grandes."
                });
            }

            if (modelo.PedidosPendientes > 0)
            {
                recomendaciones.Add(new RecomendacionIAItem
                {
                    Tipo = "info",
                    Icono = "fa-cart-shopping",
                    Titulo = "Dar seguimiento a pedidos",
                    Mensaje = $"Hay {modelo.PedidosPendientes} pedido(s) pendientes o en proceso. Conviene revisarlos para evitar retrasos."
                });
            }

            if (modelo.FacturasPendientes > 0)
            {
                recomendaciones.Add(new RecomendacionIAItem
                {
                    Tipo = "warning",
                    Icono = "fa-file-invoice-dollar",
                    Titulo = "Revisar cobros pendientes",
                    Mensaje = $"Hay {modelo.FacturasPendientes} factura(s) pendientes de pago. Se recomienda seguimiento administrativo."
                });
            }

            if (modelo.VentasTotales <= 0)
            {
                recomendaciones.Add(new RecomendacionIAItem
                {
                    Tipo = "danger",
                    Icono = "fa-chart-line",
                    Titulo = "Ventas bajas",
                    Mensaje = "No se registran ventas en el periodo seleccionado. Se recomienda revisar pedidos, facturación o estrategia comercial."
                });
            }

            if (!modelo.ProductosMasVendidos.Any())
            {
                recomendaciones.Add(new RecomendacionIAItem
                {
                    Tipo = "info",
                    Icono = "fa-mug-hot",
                    Titulo = "Sin producto dominante",
                    Mensaje = "No hay suficiente historial para determinar productos más vendidos."
                });
            }

            if (!recomendaciones.Any())
            {
                recomendaciones.Add(new RecomendacionIAItem
                {
                    Tipo = "success",
                    Icono = "fa-circle-check",
                    Titulo = "Operación estable",
                    Mensaje = "No se detectan riesgos importantes. El sistema muestra un comportamiento administrativo estable."
                });
            }

            return recomendaciones;
        }
    }
}