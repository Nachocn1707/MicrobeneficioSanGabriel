namespace MicrobeneficioSanGabriel.Models
{
    public class MetodoPago
    {
        public int? Id { get; set; }

        public string? UsuarioId { get; set; } 

        public ApplicationUser? Usuario { get; set; }

        public string? Alias { get; set; }

        public string? Ultimos4Digitos { get; set; }

        public string? Tipo { get; set; }
    }
}
