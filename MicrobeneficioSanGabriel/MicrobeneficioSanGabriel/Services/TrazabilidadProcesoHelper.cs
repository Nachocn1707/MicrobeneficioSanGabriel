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
        /// Sincroniza solamente etapas que constan realmente como registros de producción.
        /// IMPORTANTE (BUG-001): leer los listados nunca elimina producciones, ni da por
        /// completadas etapas cuya evidencia no existe. No corrige datos históricos erróneos;
        /// dichos datos requieren una revisión y ajuste administrativo con respaldo.
        /// </summary>
        public static async Task ReconciliarAsync(
            ApplicationDbContext context,
            ClaimsPrincipal usuario)
        {
            var producciones = await context.Producciones
                .AsNoTracking()
                .Where(p => p.Estado != EstadosProduccion.Cancelado)
                .ToListAsync();

            foreach (var produccion in producciones)
            {
                if (!EsEtapaDeProduccionValida(produccion.TipoProceso))
                {
                    continue;
                }

                var registrada = await context.Trazabilidades
                    .AsNoTracking()
                    .AnyAsync(t => t.LoteId == produccion.LoteId &&
                                   t.Etapa == produccion.TipoProceso &&
                                   t.EsAutomatico);
                var pendiente = context.Trazabilidades.Local.Any(t =>
                    t.LoteId == produccion.LoteId &&
                    string.Equals(t.Etapa, produccion.TipoProceso, StringComparison.OrdinalIgnoreCase) &&
                    t.EsAutomatico && context.Entry(t).State == EntityState.Added);

                if (!registrada && !pendiente)
                {
                    await SincronizarAsync(context, usuario, produccion);
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
