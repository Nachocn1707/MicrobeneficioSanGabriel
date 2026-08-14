using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Constants;
using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Services
{
    /// <summary>
    /// Centraliza la secuencia real del proceso y genera trazabilidad únicamente
    /// a partir de producciones completadas, evitando estados manuales incoherentes.
    /// </summary>
    public static class TrazabilidadProcesoHelper
    {
        public static readonly string[] EtapasOrdenadas =
        {
            "Lavado", "Secado", "Tostado", "Molido", "Empaque"
        };

        public static bool EsEtapaDeProduccionValida(string? etapa) =>
            !string.IsNullOrWhiteSpace(etapa) &&
            EtapasOrdenadas.Contains(etapa, StringComparer.OrdinalIgnoreCase);

        private static bool EsEstadoCompletado(string? estado) =>
            string.Equals(estado, EstadosProduccion.Completado, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(estado, "Completada", StringComparison.OrdinalIgnoreCase);

        public static async Task<string?> ValidarSecuenciaAsync(
            ApplicationDbContext context,
            Produccion produccion,
            int? excluirProduccionId = null)
        {
            if (!EsEtapaDeProduccionValida(produccion.TipoProceso))
            {
                return "Seleccione un tipo de proceso válido.";
            }

            var etapa = EtapasOrdenadas.First(e =>
                string.Equals(e, produccion.TipoProceso, StringComparison.OrdinalIgnoreCase));
            var indice = Array.IndexOf(EtapasOrdenadas, etapa);

            if (indice == 0)
            {
                return null;
            }

            var completadas = await context.Producciones
                .AsNoTracking()
                .Where(p => p.LoteId == produccion.LoteId &&
                            (p.Estado == EstadosProduccion.Completado || p.Estado == "Completada") &&
                            (!excluirProduccionId.HasValue || p.Id != excluirProduccionId.Value))
                .Select(p => p.TipoProceso)
                .ToListAsync();

            for (var i = 0; i < indice; i++)
            {
                if (!completadas.Any(p => string.Equals(p, EtapasOrdenadas[i], StringComparison.OrdinalIgnoreCase)))
                {
                    return $"Antes de registrar {etapa} debe completar la etapa {EtapasOrdenadas[i]} del mismo lote.";
                }
            }

            return null;
        }

        public static async Task<string?> ValidarCambioTipoAsync(
            ApplicationDbContext context,
            Produccion produccionActual,
            string? nuevoTipoProceso)
        {
            if (string.Equals(produccionActual.TipoProceso, nuevoTipoProceso, StringComparison.OrdinalIgnoreCase) ||
                !EsEtapaDeProduccionValida(produccionActual.TipoProceso))
            {
                return null;
            }

            var etapaActual = EtapasOrdenadas.First(e =>
                string.Equals(e, produccionActual.TipoProceso, StringComparison.OrdinalIgnoreCase));
            var indice = Array.IndexOf(EtapasOrdenadas, etapaActual);
            if (indice < 0 || indice == EtapasOrdenadas.Length - 1)
            {
                return null;
            }

            var posteriores = EtapasOrdenadas.Skip(indice + 1).ToArray();
            var existePosterior = await context.Producciones
                .AsNoTracking()
                .AnyAsync(p => p.LoteId == produccionActual.LoteId &&
                               p.Id != produccionActual.Id &&
                               p.Estado != EstadosProduccion.Cancelado &&
                               p.TipoProceso != null &&
                               posteriores.Contains(p.TipoProceso));

            return existePosterior
                ? $"No puede cambiar la etapa {etapaActual} porque el lote ya tiene procesos posteriores registrados."
                : null;
        }

        public static async Task<string?> ValidarEliminacionAsync(
            ApplicationDbContext context,
            Produccion produccion)
        {
            if (!EsEtapaDeProduccionValida(produccion.TipoProceso))
            {
                return null;
            }

            var etapa = EtapasOrdenadas.First(e =>
                string.Equals(e, produccion.TipoProceso, StringComparison.OrdinalIgnoreCase));
            var indice = Array.IndexOf(EtapasOrdenadas, etapa);
            if (indice < 0 || indice == EtapasOrdenadas.Length - 1)
            {
                return null;
            }

            var posteriores = EtapasOrdenadas.Skip(indice + 1).ToArray();
            var existePosterior = await context.Producciones
                .AsNoTracking()
                .AnyAsync(p => p.LoteId == produccion.LoteId &&
                               p.Id != produccion.Id &&
                               p.Estado != EstadosProduccion.Cancelado &&
                               p.TipoProceso != null &&
                               posteriores.Contains(p.TipoProceso));

            return existePosterior
                ? $"No puede eliminar {etapa} porque el lote ya tiene procesos posteriores registrados. Elimine o cancele primero las etapas posteriores para conservar una trazabilidad coherente."
                : null;
        }

        public static async Task<string?> ValidarRetrocesoAsync(
            ApplicationDbContext context,
            Produccion produccion,
            string nuevoEstado)
        {
            if (!EsEstadoCompletado(produccion.Estado) ||
                EsEstadoCompletado(nuevoEstado) ||
                !EsEtapaDeProduccionValida(produccion.TipoProceso))
            {
                return null;
            }

            var etapa = EtapasOrdenadas.First(e =>
                string.Equals(e, produccion.TipoProceso, StringComparison.OrdinalIgnoreCase));
            var indice = Array.IndexOf(EtapasOrdenadas, etapa);
            if (indice < 0 || indice == EtapasOrdenadas.Length - 1)
            {
                return null;
            }

            var posteriores = EtapasOrdenadas.Skip(indice + 1).ToArray();
            var existePosterior = await context.Producciones
                .AsNoTracking()
                .AnyAsync(p => p.LoteId == produccion.LoteId &&
                               p.Id != produccion.Id &&
                               p.Estado != EstadosProduccion.Cancelado &&
                               p.TipoProceso != null &&
                               posteriores.Contains(p.TipoProceso));

            return existePosterior
                ? $"No puede devolver {etapa} a un estado anterior porque el lote ya tiene procesos posteriores registrados."
                : null;
        }

        public static async Task SincronizarAsync(
            ApplicationDbContext context,
            ClaimsPrincipal usuario,
            Produccion produccion,
            string? responsableForzado = null,
            bool preservarResponsableExistente = false)
        {
            if (string.Equals(produccion.Estado, EstadosProduccion.Cancelado, StringComparison.OrdinalIgnoreCase) ||
                !EsEtapaDeProduccionValida(produccion.TipoProceso))
            {
                return;
            }

            var responsable = !string.IsNullOrWhiteSpace(responsableForzado)
                ? responsableForzado
                : await ObtenerNombreUsuarioAsync(context, usuario);

            var etapaActual = EtapasOrdenadas.First(e =>
                string.Equals(e, produccion.TipoProceso, StringComparison.OrdinalIgnoreCase));

            var obsTexto = $"Seguimiento automático de producción #{produccion.Id} ({etapaActual} - {produccion.Estado}).";

            // Buscar si ya existe un registro de trazabilidad para este lote en esta etapa específica
            var trazabilidadExistente = await context.Trazabilidades
                .FirstOrDefaultAsync(t => t.LoteId == produccion.LoteId && t.Etapa == etapaActual && t.EsAutomatico);

            if (trazabilidadExistente == null)
            {
                context.Trazabilidades.Add(new Trazabilidad
                {
                    LoteId = produccion.LoteId,
                    ProduccionId = produccion.Id,
                    Etapa = etapaActual,
                    FechaRegistro = produccion.FechaProduccion == default
                        ? DateTime.UtcNow.AddHours(-6)
                        : produccion.FechaProduccion,
                    Responsable = responsable,
                    Observacion = obsTexto,
                    EsAutomatico = true
                });
            }
            else
            {
                trazabilidadExistente.ProduccionId = produccion.Id;
                trazabilidadExistente.FechaRegistro = produccion.FechaProduccion == default
                    ? trazabilidadExistente.FechaRegistro
                    : produccion.FechaProduccion;

                if (!preservarResponsableExistente || string.IsNullOrWhiteSpace(trazabilidadExistente.Responsable))
                {
                    trazabilidadExistente.Responsable = responsable;
                }
                trazabilidadExistente.Observacion = obsTexto;
            }
        }

        /// <summary>
        /// Consolida producciones duplicadas dejando 1 solo proceso activo por lote en la tabla de producciones,
        /// y garantiza que la trazabilidad conserve la trayectoria completa de todas las etapas completadas del lote.
        /// </summary>
        public static async Task ReconciliarAsync(
            ApplicationDbContext context,
            ClaimsPrincipal usuario)
        {
            // 1. Consolidar registros de producción duplicados por lote (dejar 1 solo proceso por lote con la etapa más avanzada)
            var lotesConDuplicados = await context.Producciones
                .Where(p => p.Estado != EstadosProduccion.Cancelado)
                .GroupBy(p => p.LoteId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToListAsync();

            foreach (var loteId in lotesConDuplicados)
            {
                var produccionesLote = await context.Producciones
                    .Where(p => p.LoteId == loteId && p.Estado != EstadosProduccion.Cancelado)
                    .ToListAsync();

                if (produccionesLote.Count > 1)
                {
                    var produccionPrincipal = produccionesLote
                        .OrderByDescending(p => Array.IndexOf(EtapasOrdenadas, EtapasOrdenadas.FirstOrDefault(e => string.Equals(e, p.TipoProceso, StringComparison.OrdinalIgnoreCase)) ?? ""))
                        .ThenByDescending(p => p.FechaProduccion)
                        .ThenByDescending(p => p.Id)
                        .First();

                    var produccionesObs = produccionesLote
                        .Where(p => p.Id != produccionPrincipal.Id)
                        .ToList();

                    // Reasignar la referencia de trazabilidad a la producción principal antes de remover duplicados
                    var idsEliminar = produccionesObs.Select(p => p.Id).ToList();
                    var trazabilidadesAsociadas = await context.Trazabilidades
                        .Where(t => idsEliminar.Contains(t.ProduccionId))
                        .ToListAsync();

                    foreach (var t in trazabilidadesAsociadas)
                    {
                        t.ProduccionId = produccionPrincipal.Id;
                    }

                    context.Producciones.RemoveRange(produccionesObs);
                }
            }

            await context.SaveChangesAsync();

            // 2. Reconstruir la trayectoria completa de trazabilidad para cada lote según la etapa alcanzada
            var produccionesVigentes = await context.Producciones
                .Where(p => p.Estado != EstadosProduccion.Cancelado)
                .ToListAsync();

            var responsableDefault = await ObtenerNombreUsuarioAsync(context, usuario);

            foreach (var prod in produccionesVigentes)
            {
                if (!EsEtapaDeProduccionValida(prod.TipoProceso)) continue;

                var etapaActualNombre = EtapasOrdenadas.First(e => string.Equals(e, prod.TipoProceso, StringComparison.OrdinalIgnoreCase));
                var idxActual = Array.IndexOf(EtapasOrdenadas, etapaActualNombre);

                // Garantizar que existan registros de trazabilidad para la etapa actual y todas las etapas previas completadas
                for (int i = 0; i <= idxActual; i++)
                {
                    var etapaNombre = EtapasOrdenadas[i];
                    var trazabilidadEta = await context.Trazabilidades
                        .FirstOrDefaultAsync(t => t.LoteId == prod.LoteId && t.Etapa == etapaNombre && t.EsAutomatico);

                    if (trazabilidadEta == null)
                    {
                        var estadoSub = (i == idxActual) ? prod.Estado : EstadosProduccion.Completado;
                        context.Trazabilidades.Add(new Trazabilidad
                        {
                            LoteId = prod.LoteId,
                            ProduccionId = prod.Id,
                            Etapa = etapaNombre,
                            FechaRegistro = prod.FechaProduccion == default
                                ? DateTime.UtcNow.AddHours(-6)
                                : prod.FechaProduccion,
                            Responsable = responsableDefault,
                            Observacion = $"Seguimiento automático de producción #{prod.Id} ({etapaNombre} - {estadoSub}).",
                            EsAutomatico = true
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task<string> ObtenerNombreUsuarioAsync(
            ApplicationDbContext context,
            ClaimsPrincipal usuario)
        {
            var usuarioId = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(usuarioId))
            {
                var appUser = await context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == usuarioId);
                if (appUser != null)
                {
                    return appUser.NombreCompleto;
                }
            }

            return usuario.Identity?.Name ?? "Sistema";
        }
    }
}
