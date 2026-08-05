using System.ComponentModel.DataAnnotations;
using MicrobeneficioSanGabriel.Infrastructure;

namespace MicrobeneficioSanGabriel.Models
{
    public class Productor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s.'-]+$", ErrorMessage = "El nombre contiene caracteres no permitidos")]
        public string? Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [NotWhiteSpace(ErrorMessage = "La cédula es obligatoria")]
        [StringLength(12, MinimumLength = 9, ErrorMessage = "La cédula debe tener entre 9 y 12 dígitos")]
        [RegularExpression(@"^\d{9,12}$", ErrorMessage = "La cédula debe contener únicamente números")]
        public string? Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El teléfono debe contener exactamente 8 dígitos, por ejemplo 88888888")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El correo electrónico es obligatorio")]
        [CompleteEmailAddress]
        [DataType(DataType.EmailAddress)]
        [StringLength(150)]
        [Display(Name = "Correo electrónico")]
        public string? Correo { get; set; }

        [Required(ErrorMessage = "Seleccione una provincia")]
        [NotWhiteSpace(ErrorMessage = "Seleccione una provincia")]
        [StringLength(50)]
        public string? Provincia { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cantón es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El cantón es obligatorio")]
        [StringLength(80)]
        [Display(Name = "Cantón")]
        public string? Canton { get; set; } = string.Empty;

        [Required(ErrorMessage = "El distrito es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El distrito es obligatorio")]
        [StringLength(80)]
        public string? Distrito { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección exacta es obligatoria")]
        [NotWhiteSpace(ErrorMessage = "La dirección exacta es obligatoria")]
        [StringLength(250, MinimumLength = 5)]
        [Display(Name = "Dirección exacta")]
        public string? DireccionExacta { get; set; } = string.Empty;

        // Campo heredado para conservar compatibilidad con bases de datos existentes.
        // El controlador lo mantiene sincronizado con los campos de ubicación.
        [StringLength(300)]
        public string? Direccion { get; set; } = string.Empty;

        // Campo heredado. Las fincas nuevas se gestionan mediante la entidad Finca.
        [StringLength(100)]
        public string? Finca { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public ICollection<Finca> Fincas { get; set; } = new List<Finca>();
        public ICollection<Lote> Lotes { get; set; } = new List<Lote>();

        public string UbicacionCompleta =>
            string.Join(", ", new[] { Distrito, Canton, Provincia }
                .Where(valor => !string.IsNullOrWhiteSpace(valor)));
    }
}
