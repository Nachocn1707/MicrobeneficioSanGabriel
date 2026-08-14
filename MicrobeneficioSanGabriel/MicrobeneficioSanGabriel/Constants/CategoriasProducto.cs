namespace MicrobeneficioSanGabriel.Constants
{
    public static class CategoriasProducto
    {
        public static readonly string[] Todas =
        {
            "Café en Grano",
            "Café Molido",
            "Café Tostado",
            "Café Verde",
            "Café Gourmet",
            "Café Especialidad",
            "Capuchino"
        };

        public static bool EsValida(string? categoria) =>
            !string.IsNullOrWhiteSpace(categoria) &&
            Todas.Contains(categoria.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
