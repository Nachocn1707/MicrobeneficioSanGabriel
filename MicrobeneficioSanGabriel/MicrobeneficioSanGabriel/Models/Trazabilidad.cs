using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class Trazabilidad
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un lote")]
        public int LoteId { get; set; }

        [ForeignKey("LoteId")]
        public Lote? Lote { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una producción")]
        public int ProduccionId { get; set; }

        [ForeignKey("ProduccionId")]
        public Produccion? Produccion { get; set; }

        [Required(ErrorMessage = "La etapa es obligatoria")]
        public string Etapa { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public string? Responsable { get; set; }

        public string? Observacion { get; set; }
    }
}