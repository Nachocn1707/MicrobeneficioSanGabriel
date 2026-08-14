namespace MicrobeneficioSanGabriel.Constants
{
    public static class CategoriasFinancieras
    {
        public static readonly string[] Manuales =
        {
            "Mantenimiento",
            "Transporte",
            "Nómina"
        };

        public static bool EsManualValida(string? categoria) =>
            !string.IsNullOrWhiteSpace(categoria) &&
            Manuales.Contains(categoria.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
