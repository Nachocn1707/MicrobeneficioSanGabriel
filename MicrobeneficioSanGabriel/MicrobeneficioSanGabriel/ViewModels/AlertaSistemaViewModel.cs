namespace MicrobeneficioSanGabriel.ViewModels
{
    public class AlertaSistemaViewModel
    {
        public string Clave { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Tipo { get; set; } = "info";
        public string Icono { get; set; } = "fa-circle-info";
        public string Url { get; set; } = "#";
        public int Prioridad { get; set; } = 99;
        public DateTime FechaReferencia { get; set; } = DateTime.Now;
        public string Tiempo { get; set; } = "Hace un momento";
    }
}