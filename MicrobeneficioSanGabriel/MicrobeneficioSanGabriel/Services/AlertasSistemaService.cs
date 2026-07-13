using MicrobeneficioSanGabriel.Constants;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MicrobeneficioSanGabriel.Services
{
    public class AlertasSistemaService : IAlertasSistemaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAnalisisInventarioIAService _analisisInventarioIAService;

        public AlertasSistemaService(
            ApplicationDbContext context,
            IAnalisisInventarioIAService analisisInventarioIAService)
        {
            _context = context;
            _analisisInventarioIAService = analisisInventarioIAService;
        }

        public async Task<List<AlertaSistemaViewModel>> ObtenerAlertasAsync(ClaimsPrincipal user)
        {
            var alertas = new List<AlertaSistemaViewModel>();
            var hoy = DateTime.Now;
            var fechaLimite = hoy.AddDays(-7);

            if (user.IsInRole("Cliente"))
            {
                var clienteId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var correoCliente = (user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.Identity?.Name
                    ?? string.Empty).Trim().ToLowerInvariant();

                var pedidosCliente = await _context.Pedidos
                    .Include(p => p.Producto)
                    .AsNoTracking()
                    .Where(p => p.ClienteId == clienteId ||
                                (p.ClienteId == null && p.ClienteCorreo != null && p.ClienteCorreo.ToLower() == correoCliente))
                    .OrderByDescending(p => p.FechaPedido)
                    .ToListAsync();

                var pedidosPendientesCliente = pedidosCliente
                    .Where(p => p.Estado == "Pendiente" || p.Estado == "En proceso")
                    .ToList();

                if (pedidosPendientesCliente.Any())
                {
                    var pedidoReferencia = pedidosPendientesCliente.First();
                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"cliente-pedidos-pendientes-{pedidoReferencia.Id}-{pedidosPendientesCliente.Count}",
                        Modulo = "Mis pedidos",
                        Titulo = "Pedido en seguimiento",
                        Mensaje = pedidosPendientesCliente.Count == 1
                            ? "Tenés 1 pedido pendiente o en proceso."
                            : $"Tenés {pedidosPendientesCliente.Count} pedidos pendientes o en proceso.",
                        Tipo = "info",
                        Icono = "fa-clipboard-list",
                        Url = "/Pedidos",
                        Prioridad = 1,
                        FechaReferencia = pedidoReferencia.FechaPedido,
                        Tiempo = TiempoRelativo(pedidoReferencia.FechaPedido)
                    });
                }

                var pedidosCompletadosCliente = pedidosCliente
                    .Where(p => p.Estado == "Completado")
                    .ToList();

                if (pedidosCompletadosCliente.Any())
                {
                    var pedidoReferencia = pedidosCompletadosCliente.First();
                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"cliente-pedidos-completados-{pedidoReferencia.Id}-{pedidosCompletadosCliente.Count}",
                        Modulo = "Mis pedidos",
                        Titulo = "Pedido completado",
                        Mensaje = pedidosCompletadosCliente.Count == 1
                            ? "Tenés 1 pedido completado listo para revisar."
                            : $"Tenés {pedidosCompletadosCliente.Count} pedidos completados.",
                        Tipo = "success",
                        Icono = "fa-circle-check",
                        Url = "/Pedidos",
                        Prioridad = 2,
                        FechaReferencia = pedidoReferencia.FechaPedido,
                        Tiempo = TiempoRelativo(pedidoReferencia.FechaPedido)
                    });
                }

                var pagosPendientesCliente = pedidosCliente
                    .Where(p => EstadosPago.EsPendiente(p.EstadoPago))
                    .ToList();

                if (pagosPendientesCliente.Any())
                {
                    var pedidoReferencia = pagosPendientesCliente.First();
                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"cliente-pagos-pendientes-{pedidoReferencia.Id}-{pagosPendientesCliente.Count}",
                        Modulo = "Pagos",
                        Titulo = "Pago pendiente",
                        Mensaje = pagosPendientesCliente.Count == 1
                            ? "Tenés 1 pedido con pago pendiente."
                            : $"Tenés {pagosPendientesCliente.Count} pedidos con pago pendiente.",
                        Tipo = "warning",
                        Icono = "fa-wallet",
                        Url = "/Pedidos",
                        Prioridad = 3,
                        FechaReferencia = pedidoReferencia.FechaPedido,
                        Tiempo = TiempoRelativo(pedidoReferencia.FechaPedido)
                    });
                }

                var ultimoPedido = pedidosCliente.FirstOrDefault();
                if (ultimoPedido != null && !alertas.Any())
                {
                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"cliente-ultimo-pedido-{ultimoPedido.Id}-{ultimoPedido.Estado}",
                        Modulo = "Mis pedidos",
                        Titulo = "Estado de tu pedido",
                        Mensaje = $"Tu último pedido está en estado: {ultimoPedido.Estado}.",
                        Tipo = "info",
                        Icono = "fa-mug-hot",
                        Url = "/Pedidos",
                        Prioridad = 4,
                        FechaReferencia = ultimoPedido.FechaPedido,
                        Tiempo = TiempoRelativo(ultimoPedido.FechaPedido)
                    });
                }

                return await FiltrarDescartadasAsync(user, alertas);
            }

            if (user.IsInRole("Administrador") || user.IsInRole("Operador") || user.IsInRole("Vendedor"))
            {
                var productosStockBajo = await _context.Productos
                    .Where(p => p.Stock <= p.StockMinimo)
                    .OrderBy(p => p.Stock)
                    .ToListAsync();

                if (productosStockBajo.Any())
                {
                    var productoReferencia = productosStockBajo.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"productos-stock-bajo-{productoReferencia.Id}-{productosStockBajo.Count}",
                        Modulo = "Productos",
                        Titulo = "Productos con stock bajo",
                        Mensaje = $"Hay {productosStockBajo.Count} producto(s) por debajo del stock mínimo.",
                        Tipo = "warning",
                        Icono = "fa-boxes-stacked",
                        Url = "/Productos",
                        Prioridad = 1,
                        FechaReferencia = productoReferencia.FechaRegistro,
                        Tiempo = TiempoRelativo(productoReferencia.FechaRegistro)
                    });
                }
            }

            if (user.IsInRole("Administrador") || user.IsInRole("Operador"))
            {
                var alertasIA = await _analisisInventarioIAService.GenerarAlertasAsync();

                foreach (var alertaIA in alertasIA.Where(a => a.NivelRiesgo != "Estable").Take(5))
                {
                    var fechaReferencia = alertaIA.UltimoMovimiento ?? DateTime.Now;

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"ia-inventario-{alertaIA.ProductoId}-{alertaIA.NivelRiesgo}",
                        Modulo = "Análisis de inventario",
                        Titulo = alertaIA.NivelRiesgo,
                        Mensaje = alertaIA.Mensaje,
                        Tipo = alertaIA.Color,
                        Icono = alertaIA.Icono,
                        Url = "/Home/Dashboard",
                        Prioridad = alertaIA.Prioridad,
                        FechaReferencia = fechaReferencia,
                        Tiempo = TiempoRelativo(fechaReferencia)
                    });
                }

                var lotesEnProceso = await _context.Lotes
                    .Where(l => l.Estado == "En proceso")
                    .OrderByDescending(l => l.FechaRecepcion)
                    .ToListAsync();

                if (lotesEnProceso.Any())
                {
                    var loteReferencia = lotesEnProceso.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"lotes-en-proceso-{loteReferencia.Id}-{lotesEnProceso.Count}",
                        Modulo = "Lotes",
                        Titulo = "Lotes en proceso",
                        Mensaje = $"Hay {lotesEnProceso.Count} lote(s) en proceso.",
                        Tipo = "info",
                        Icono = "fa-layer-group",
                        Url = "/Lotes",
                        Prioridad = 3,
                        FechaReferencia = loteReferencia.FechaRecepcion,
                        Tiempo = TiempoRelativo(loteReferencia.FechaRecepcion)
                    });
                }

                var lotesRecibidos = await _context.Lotes
                    .Where(l => l.Estado == "Recibido" || l.Estado == "Recibidos")
                    .OrderByDescending(l => l.FechaRecepcion)
                    .ToListAsync();

                if (lotesRecibidos.Any())
                {
                    var loteReferencia = lotesRecibidos.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"lotes-recibidos-{loteReferencia.Id}-{lotesRecibidos.Count}",
                        Modulo = "Lotes",
                        Titulo = "Lotes recibidos",
                        Mensaje = $"Hay {lotesRecibidos.Count} lote(s) recibidos pendientes de proceso.",
                        Tipo = "warning",
                        Icono = "fa-sack-xmark",
                        Url = "/Lotes",
                        Prioridad = 4,
                        FechaReferencia = loteReferencia.FechaRecepcion,
                        Tiempo = TiempoRelativo(loteReferencia.FechaRecepcion)
                    });
                }

                var lotesAntiguos = await _context.Lotes
                    .Where(l => l.Estado != "Finalizado" && l.FechaRecepcion <= fechaLimite)
                    .OrderBy(l => l.FechaRecepcion)
                    .ToListAsync();

                if (lotesAntiguos.Any())
                {
                    var loteReferencia = lotesAntiguos.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"lotes-sin-finalizar-{loteReferencia.Id}-{lotesAntiguos.Count}",
                        Modulo = "Lotes",
                        Titulo = "Lotes sin finalizar",
                        Mensaje = $"Hay {lotesAntiguos.Count} lote(s) con más de 7 días sin finalizar.",
                        Tipo = "warning",
                        Icono = "fa-clock",
                        Url = "/Lotes",
                        Prioridad = 5,
                        FechaReferencia = loteReferencia.FechaRecepcion,
                        Tiempo = TiempoRelativo(loteReferencia.FechaRecepcion)
                    });
                }

                var produccionesEnProceso = await _context.Producciones
                    .Where(p => p.Estado == "En proceso")
                    .OrderByDescending(p => p.FechaProduccion)
                    .ToListAsync();

                if (produccionesEnProceso.Any())
                {
                    var produccionReferencia = produccionesEnProceso.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"producciones-en-proceso-{produccionReferencia.Id}-{produccionesEnProceso.Count}",
                        Modulo = "Producción",
                        Titulo = "Producciones en proceso",
                        Mensaje = $"Hay {produccionesEnProceso.Count} producción(es) en proceso.",
                        Tipo = "info",
                        Icono = "fa-industry",
                        Url = "/Producciones",
                        Prioridad = 6,
                        FechaReferencia = produccionReferencia.FechaProduccion,
                        Tiempo = TiempoRelativo(produccionReferencia.FechaProduccion)
                    });
                }

                var produccionesPendientes = await _context.Producciones
                    .Where(p => p.Estado == "Pendiente")
                    .OrderByDescending(p => p.FechaProduccion)
                    .ToListAsync();

                if (produccionesPendientes.Any())
                {
                    var produccionReferencia = produccionesPendientes.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"producciones-pendientes-{produccionReferencia.Id}-{produccionesPendientes.Count}",
                        Modulo = "Producción",
                        Titulo = "Producciones pendientes",
                        Mensaje = $"Hay {produccionesPendientes.Count} producción(es) pendientes.",
                        Tipo = "warning",
                        Icono = "fa-mug-hot",
                        Url = "/Producciones",
                        Prioridad = 7,
                        FechaReferencia = produccionReferencia.FechaProduccion,
                        Tiempo = TiempoRelativo(produccionReferencia.FechaProduccion)
                    });
                }

                var lotesActivos = await _context.Lotes
                    .CountAsync(l => l.Estado != "Finalizado");

                var ultimaTrazabilidad = await _context.Trazabilidades
                    .OrderByDescending(t => t.FechaRegistro)
                    .FirstOrDefaultAsync();

                var hayTrazabilidadReciente = ultimaTrazabilidad != null &&
                                              ultimaTrazabilidad.FechaRegistro >= fechaLimite;

                if (lotesActivos > 0 && !hayTrazabilidadReciente)
                {
                    var fechaReferencia = ultimaTrazabilidad?.FechaRegistro ?? DateTime.Now.AddDays(-7);

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"trazabilidad-sin-registros-{lotesActivos}-{fechaReferencia:yyyyMMddHHmmss}",
                        Modulo = "Trazabilidad",
                        Titulo = "Trazabilidad sin registros recientes",
                        Mensaje = "Hay lotes activos, pero no hay registros recientes de trazabilidad.",
                        Tipo = "warning",
                        Icono = "fa-route",
                        Url = "/Trazabilidades",
                        Prioridad = 8,
                        FechaReferencia = fechaReferencia,
                        Tiempo = TiempoRelativo(fechaReferencia)
                    });
                }
            }

            if (user.IsInRole("Administrador") || user.IsInRole("Vendedor"))
            {
                var pedidosPendientes = await _context.Pedidos
                    .Where(p => p.Estado == "Pendiente" || p.Estado == "En proceso")
                    .OrderByDescending(p => p.FechaPedido)
                    .ToListAsync();

                if (pedidosPendientes.Any())
                {
                    var pedidoReferencia = pedidosPendientes.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"pedidos-pendientes-{pedidoReferencia.Id}-{pedidosPendientes.Count}",
                        Modulo = "Pedidos",
                        Titulo = "Pedidos pendientes",
                        Mensaje = $"Hay {pedidosPendientes.Count} pedido(s) pendientes o en proceso.",
                        Tipo = "info",
                        Icono = "fa-cart-shopping",
                        Url = "/Pedidos",
                        Prioridad = 9,
                        FechaReferencia = pedidoReferencia.FechaPedido,
                        Tiempo = TiempoRelativo(pedidoReferencia.FechaPedido)
                    });
                }

                var facturasPendientes = await _context.Facturas
                    .Where(f => f.EstadoPago == EstadosPago.Pendiente ||
                                f.EstadoPago == EstadosPago.PendientePago ||
                                f.EstadoPago == "Sin pago")
                    .OrderByDescending(f => f.FechaFactura)
                    .ToListAsync();

                if (facturasPendientes.Any())
                {
                    var facturaReferencia = facturasPendientes.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"facturas-pendientes-{facturaReferencia.Id}-{facturasPendientes.Count}",
                        Modulo = "Facturación",
                        Titulo = "Facturas pendientes",
                        Mensaje = $"Hay {facturasPendientes.Count} factura(s) pendientes de pago.",
                        Tipo = "warning",
                        Icono = "fa-file-invoice-dollar",
                        Url = "/Facturas",
                        Prioridad = 10,
                        FechaReferencia = facturaReferencia.FechaFactura,
                        Tiempo = TiempoRelativo(facturaReferencia.FechaFactura)
                    });
                }

                var facturasAnuladas = await _context.Facturas
                    .Where(f => f.EstadoPago == "Anulada")
                    .OrderByDescending(f => f.FechaFactura)
                    .ToListAsync();

                if (facturasAnuladas.Any())
                {
                    var facturaReferencia = facturasAnuladas.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"facturas-anuladas-{facturaReferencia.Id}-{facturasAnuladas.Count}",
                        Modulo = "Facturación",
                        Titulo = "Facturas anuladas",
                        Mensaje = $"Hay {facturasAnuladas.Count} factura(s) anuladas en el sistema.",
                        Tipo = "danger",
                        Icono = "fa-ban",
                        Url = "/Facturas",
                        Prioridad = 11,
                        FechaReferencia = facturaReferencia.FechaFactura,
                        Tiempo = TiempoRelativo(facturaReferencia.FechaFactura)
                    });
                }
            }

            if (user.IsInRole("Administrador"))
            {
                var productoresInactivos = await _context.Productores
                    .Where(p => !p.Activo)
                    .OrderByDescending(p => p.FechaRegistro)
                    .ToListAsync();

                if (productoresInactivos.Any())
                {
                    var productorReferencia = productoresInactivos.First();

                    alertas.Add(new AlertaSistemaViewModel
                    {
                        Clave = $"productores-inactivos-{productorReferencia.Id}-{productoresInactivos.Count}",
                        Modulo = "Productores",
                        Titulo = "Productores inactivos",
                        Mensaje = $"Hay {productoresInactivos.Count} productor(es) inactivos.",
                        Tipo = "info",
                        Icono = "fa-user-slash",
                        Url = "/Productores",
                        Prioridad = 12,
                        FechaReferencia = productorReferencia.FechaRegistro,
                        Tiempo = TiempoRelativo(productorReferencia.FechaRegistro)
                    });
                }
            }

            return await FiltrarDescartadasAsync(user, alertas);
        }

        private async Task<List<AlertaSistemaViewModel>> FiltrarDescartadasAsync(
            ClaimsPrincipal user,
            IEnumerable<AlertaSistemaViewModel> alertas)
        {
            var usuarioId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var ordenadas = alertas
                .OrderBy(a => a.Prioridad)
                .ThenByDescending(a => a.FechaReferencia)
                .ToList();

            if (string.IsNullOrWhiteSpace(usuarioId) || ordenadas.Count == 0)
            {
                return ordenadas;
            }

            var limite = DateTime.Now.AddDays(-30);
            var claves = ordenadas.Select(a => a.Clave).Distinct().ToList();
            var descartadas = await _context.NotificacionesUsuarios
                .AsNoTracking()
                .Where(n => n.UsuarioId == usuarioId &&
                            n.FechaDescartada >= limite &&
                            claves.Contains(n.Clave))
                .Select(n => n.Clave)
                .ToListAsync();

            return ordenadas.Where(a => !descartadas.Contains(a.Clave)).ToList();
        }

        private static string TiempoRelativo(DateTime fecha)
        {
            var diferencia = DateTime.Now - fecha;

            if (diferencia.TotalMinutes < 1)
            {
                return "Hace un momento";
            }

            if (diferencia.TotalMinutes < 60)
            {
                var minutos = (int)diferencia.TotalMinutes;
                return minutos == 1 ? "Hace 1 minuto" : $"Hace {minutos} minutos";
            }

            if (diferencia.TotalHours < 24)
            {
                var horas = (int)diferencia.TotalHours;
                return horas == 1 ? "Hace 1 hora" : $"Hace {horas} horas";
            }

            if (diferencia.TotalDays < 7)
            {
                var dias = (int)diferencia.TotalDays;
                return dias == 1 ? "Hace 1 día" : $"Hace {dias} días";
            }

            return fecha.ToString("dd/MM/yyyy HH:mm");
        }
    }
}