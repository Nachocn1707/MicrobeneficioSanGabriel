using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.ViewModels
{
    public class TrazabilidadHistorialViewModel
    {
        public Lote? Lote { get; set; }
        public Productor? Productor { get; set; }
        public List<Produccion> Producciones { get; set; } = new();
        public List<Trazabilidad> Trazabilidades { get; set; } = new();
    }
}