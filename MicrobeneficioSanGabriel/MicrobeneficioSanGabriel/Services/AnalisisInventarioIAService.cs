using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MicrobeneficioSanGabriel.Services
{
    public class AnalisisInventarioIAService : IAnalisisInventarioIAService
    {
        private readonly ApplicationDbContext _context;

        public AnalisisInventarioIAService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AlertaInventarioIAViewModel>> GenerarAlertasAsync()
        {
            var fechaHoy = DateTime.UtcNow.AddHours(-6);
            var fecha30Dias = fechaHoy.AddDays(-30);
            var fecha45Dias = fechaHoy.AddDays(-45);

            var productos = await _context.Productos
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            // Solo se analizan movimientos que todavía pertenecen a productos activos.
            // Los movimientos históricos cuyo producto fue eliminado conservan su nombre,
            // pero tienen ProductoId = NULL y no deben materializarse para este análisis.
            var movimientos = await (
                from movimiento in _context.MovimientosInventario.AsNoTracking()
                join producto in _context.Productos.AsNoTracking()
                    on movimiento.ProductoId equals (int?)producto.Id
                where movimiento.FechaMovimiento >= fecha45Dias
                select new
                {
                    ProductoId = producto.Id,
                    movimiento.TipoMovimiento,
                    movimiento.Cantidad,
                    movimiento.FechaMovimiento
                })
                .ToListAsync();

            var alertas = new List<AlertaInventarioIAViewModel>();

            foreach (var producto in productos)
            {
                var movimientosProducto = movimientos
                    .Where(m => m.ProductoId == producto.Id)
                    .ToList();

                var salidas30 = movimientosProducto
                    .Where(m => m.TipoMovimiento == "Salida" && m.FechaMovimiento >= fecha30Dias)
                    .Sum(m => m.Cantidad);

                var entradas30 = movimientosProducto
                    .Where(m => m.TipoMovimiento == "Entrada" && m.FechaMovimiento >= fecha30Dias)
                    .Sum(m => m.Cantidad);

                var ultimoMovimiento = movimientosProducto
                    .OrderByDescending(m => m.FechaMovimiento)
                    .FirstOrDefault();

                var consumoDiario = salidas30 > 0 ? salidas30 / 30m : 0;

                int? diasParaAgotarse = consumoDiario > 0
                    ? (int)Math.Ceiling(producto.Stock / consumoDiario)
                    : null;

                var alerta = new AlertaInventarioIAViewModel
                {
                    ProductoId = producto.Id,
                    ProductoNombre = producto.Nombre,
                    StockActual = producto.Stock,
                    StockMinimo = producto.StockMinimo,
                    SalidasUltimos30Dias = salidas30,
                    EntradasUltimos30Dias = entradas30,
                    ConsumoDiarioPromedio = consumoDiario,
                    DiasParaAgotarse = diasParaAgotarse,
                    UltimoMovimiento = ultimoMovimiento?.FechaMovimiento
                };

                if (producto.Stock <= 0)
                {
                    alerta.NivelRiesgo = "Crítico";
                    alerta.Color = "danger";
                    alerta.Icono = "fa-triangle-exclamation";
                    alerta.Prioridad = 1;
                    alerta.Mensaje = $"El producto {producto.Nombre} está agotado.";
                    alerta.Recomendacion = "Recomendación: producir o reabastecer este producto de forma inmediata.";
                }
                else if (producto.Stock <= producto.StockMinimo)
                {
                    alerta.NivelRiesgo = "Alto";
                    alerta.Color = "danger";
                    alerta.Icono = "fa-arrow-trend-down";
                    alerta.Prioridad = 2;
                    alerta.Mensaje = $"El producto {producto.Nombre} está por debajo del stock mínimo.";
                    alerta.Recomendacion = "Recomendación: aumentar inventario antes de aceptar nuevos pedidos grandes.";
                }
                else if (diasParaAgotarse.HasValue && diasParaAgotarse.Value <= 7)
                {
                    alerta.NivelRiesgo = "Alto";
                    alerta.Color = "warning";
                    alerta.Icono = "fa-clock";
                    alerta.Prioridad = 3;
                    alerta.Mensaje = $"El producto {producto.Nombre} tiene alta salida y podría agotarse en {diasParaAgotarse} días.";
                    alerta.Recomendacion = "Recomendación: programar producción o compra durante esta semana.";
                }
                else if (salidas30 > 0 && salidas30 >= producto.Stock * 0.70m)
                {
                    alerta.NivelRiesgo = "Medio";
                    alerta.Color = "warning";
                    alerta.Icono = "fa-chart-line";
                    alerta.Prioridad = 4;
                    alerta.Mensaje = $"El producto {producto.Nombre} tuvo una salida alta en los últimos 30 días.";
                    alerta.Recomendacion = "Recomendación: revisar el stock mínimo y considerar aumentarlo.";
                }
                else if (ultimoMovimiento == null || ultimoMovimiento.FechaMovimiento < fecha45Dias)
                {
                    alerta.NivelRiesgo = "Bajo movimiento";
                    alerta.Color = "info";
                    alerta.Icono = "fa-box-open";
                    alerta.Prioridad = 5;
                    alerta.Mensaje = $"El producto {producto.Nombre} no ha tenido movimientos recientes.";
                    alerta.Recomendacion = "Recomendación: revisar estrategia de venta o rotación del producto.";
                }
                else
                {
                    alerta.NivelRiesgo = "Estable";
                    alerta.Color = "success";
                    alerta.Icono = "fa-circle-check";
                    alerta.Prioridad = 6;
                    alerta.Mensaje = $"El producto {producto.Nombre} mantiene un comportamiento estable.";
                    alerta.Recomendacion = "Recomendación: continuar monitoreando el inventario normalmente.";
                }

                alertas.Add(alerta);
            }

            return alertas
                .OrderBy(a => a.Prioridad)
                .ThenBy(a => a.DiasParaAgotarse ?? 9999)
                .ToList();
        }
    }
}