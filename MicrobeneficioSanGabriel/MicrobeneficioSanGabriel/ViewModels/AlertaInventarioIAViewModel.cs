namespace MicrobeneficioSanGabriel.ViewModels
{
    public class AlertaInventarioIAViewModel
    {
        public int ProductoId { get; set; }

        public string ProductoNombre { get; set; } = string.Empty;

        public decimal StockActual { get; set; }

        public decimal StockMinimo { get; set; }

        public decimal SalidasUltimos30Dias { get; set; }

        public decimal EntradasUltimos30Dias { get; set; }

        public decimal ConsumoDiarioPromedio { get; set; }

        public int? DiasParaAgotarse { get; set; }

        public DateTime? UltimoMovimiento { get; set; }

        public string NivelRiesgo { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Icono { get; set; } = string.Empty;

        public string Mensaje { get; set; } = string.Empty;

        public string Recomendacion { get; set; } = string.Empty;

        public int Prioridad { get; set; }
    }
}