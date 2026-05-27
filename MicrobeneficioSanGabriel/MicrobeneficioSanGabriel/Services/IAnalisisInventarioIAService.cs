using MicrobeneficioSanGabriel.ViewModels;

namespace MicrobeneficioSanGabriel.Services
{
    public interface IAnalisisInventarioIAService
    {
        Task<List<AlertaInventarioIAViewModel>> GenerarAlertasAsync();
    }
}