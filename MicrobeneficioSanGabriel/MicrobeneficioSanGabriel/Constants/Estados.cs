namespace MicrobeneficioSanGabriel.Constants
{
    public static class EstadosPedido
    {
        public const string Pendiente = "Pendiente";
        public const string EnProceso = "En proceso";
        public const string EnCamino = "En camino";             
        public const string ListoParaRetirar = "Listo para retirar"; 
        public const string Completado = "Completado";
        public const string Cancelado = "Cancelado";

        public static readonly string[] Permitidos =
        {
            Pendiente, EnProceso, EnCamino, ListoParaRetirar, Completado, Cancelado
        };

        public static bool EsValido(string? estado) =>
            !string.IsNullOrWhiteSpace(estado) && Permitidos.Contains(estado);
    }

    public static class EstadosPago
    {
        public const string Pendiente = "Pendiente";
        public const string PendientePago = "Pendiente de pago";
        public const string PagoCompletado = "Pago completado";
        public const string Anulada = "Anulada";
        public const string Cancelado = "Cancelado";

        public static readonly string[] Editables =
        {
            Pendiente, PendientePago, PagoCompletado
        };

        public static bool EsPagado(string? estado) =>
            string.Equals(estado, PagoCompletado, StringComparison.OrdinalIgnoreCase);

        public static bool EsPendiente(string? estado) =>
            string.IsNullOrWhiteSpace(estado) ||
            string.Equals(estado, Pendiente, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(estado, PendientePago, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(estado, "Sin pago", StringComparison.OrdinalIgnoreCase);
    }

    public static class EstadosProduccion
    {
        public const string EnProceso = "En proceso";
        public const string Completado = "Completado";
        public const string Cancelado = "Cancelado";

        public static bool EsValido(string? estado) =>
            estado is EnProceso or Completado or Cancelado;
    }

    public static class EstadosLote
    {
        public const string Recibido = "Recibido";
        public const string EnProceso = "En proceso";
        public const string Finalizado = "Finalizado";
        public const string Cancelado = "Cancelado";

        public static bool EsValido(string? estado) =>
            estado is Recibido or EnProceso or Finalizado or Cancelado;
    }

    public static class OrigenMovimiento
    {
        public const string Pedido = "Pedido";
        public const string Produccion = "Produccion";
        public const string ReversionPedido = "ReversionPedido";
        public const string ReversionProduccion = "ReversionProduccion";
    }
}