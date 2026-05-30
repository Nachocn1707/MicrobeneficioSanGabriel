namespace MicrobeneficioSanGabriel.ViewModels
{
    public class ReporteIAViewModel
    {
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public decimal VentasTotales { get; set; }
        public int TotalPedidos { get; set; }
        public int TotalFacturas { get; set; }
        public int PedidosPendientes { get; set; }
        public int PedidosCompletados { get; set; }
        public int FacturasPendientes { get; set; }
        public int ProductosStockBajo { get; set; }
        public decimal StockTotal { get; set; }

        public string ResumenEjecutivo { get; set; } = string.Empty;

        public List<ProductoVendidoIAItem> ProductosMasVendidos { get; set; } = new();
        public List<ClienteFrecuenteIAItem> ClientesFrecuentes { get; set; } = new();
        public List<EstadoPedidoIAItem> EstadosPedidos { get; set; } = new();
        public List<RecomendacionIAItem> Recomendaciones { get; set; } = new();
    }

    public class ProductoVendidoIAItem
    {
        public string Producto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal TotalVentas { get; set; }
    }

    public class ClienteFrecuenteIAItem
    {
        public string Cliente { get; set; } = string.Empty;
        public int Pedidos { get; set; }
        public decimal TotalComprado { get; set; }
    }

    public class EstadoPedidoIAItem
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class RecomendacionIAItem
    {
        public string Tipo { get; set; } = "info";
        public string Icono { get; set; } = "fa-circle-info";
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}