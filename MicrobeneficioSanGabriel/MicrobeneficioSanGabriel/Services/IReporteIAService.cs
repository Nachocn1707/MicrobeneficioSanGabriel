using MicrobeneficioSanGabriel.ViewModels;

namespace MicrobeneficioSanGabriel.Services
{
    public interface IReporteIAService
    {
        Task<ReporteIAViewModel> GenerarAnalisisAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null);
    }
}