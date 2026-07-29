using MicrobeneficioSanGabriel.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicrobeneficioSanGabriel.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El nombre del cliente es obligatorio")]
        [Display(Name = "Cliente")]
        public string ClienteNombre { get; set; } = string.Empty;

        [Display(Name = "Cliente registrado")]
        public string? ClienteId { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public ApplicationUser? Cliente { get; set; }

        [Display(Name = "Correo del cliente")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido")]
        [StringLength(150)]
        public string? ClienteCorreo { get; set; }

        [Required(ErrorMessage = "El teléfono del cliente es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El teléfono del cliente es obligatorio")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El teléfono debe contener exactamente 8 dígitos, por ejemplo 88888888")]
        [Display(Name = "Teléfono del cliente")]
        public string ClienteTelefono { get; set; } = string.Empty;

        // Debe permanecer nullable en el modelo de EF para conservar el pedido
        // después de eliminar el producto. La selección obligatoria se valida
        // en los controladores al crear o editar pedidos.
        [Display(Name = "Producto")]
        public int? ProductoId { get; set; }

        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }

        [StringLength(100)]
        [Display(Name = "Nombre histórico del producto")]
        public string? ProductoNombre { get; set; }

        [Display(Name = "Precio unitario histórico")]
        public decimal PrecioUnitario { get; set; }

        [NotMapped]
        public string ProductoNombreMostrar =>
            Producto?.Nombre
            ?? (!string.IsNullOrWhiteSpace(ProductoNombre) ? ProductoNombre : "Producto eliminado");

        [NotMapped]
        public decimal PrecioUnitarioMostrar =>
            PrecioUnitario > 0 ? PrecioUnitario : Producto?.Precio ?? 0m;

        [NotMapped]
        public decimal SubtotalMostrar =>
            Math.Round(PrecioUnitarioMostrar * Cantidad, 2, MidpointRounding.AwayFromZero);

        [NotMapped]
        public decimal IvaMostrar =>
            Math.Round(SubtotalMostrar * 0.13m, 2, MidpointRounding.AwayFromZero);

        [NotMapped]
        public decimal TotalMostrar =>
            Math.Round(SubtotalMostrar + IvaMostrar, 2, MidpointRounding.AwayFromZero);

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, 999999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [WholeNumber]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "La fecha del pedido es obligatoria")]
        [Display(Name = "Fecha Pedido")]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El estado es obligatorio")]
        [NotWhiteSpace(ErrorMessage = "El estado es obligatorio")]
        public string Estado { get; set; } = "Pendiente";

        [Display(Name = "Observación")]
        public string? Observacion { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public string EstadoPago { get; set; } = string.Empty;

        public bool InventarioAplicado { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
