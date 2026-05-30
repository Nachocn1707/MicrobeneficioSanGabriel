using MicrobeneficioSanGabriel.ViewModels;
using System.Security.Claims;

namespace MicrobeneficioSanGabriel.Services
{
    public interface IAlertasSistemaService
    {
        Task<List<AlertaSistemaViewModel>> ObtenerAlertasAsync(ClaimsPrincipal user);
    }
}