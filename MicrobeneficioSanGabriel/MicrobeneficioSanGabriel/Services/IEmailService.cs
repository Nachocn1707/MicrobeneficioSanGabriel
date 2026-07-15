namespace MicrobeneficioSanGabriel.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Indica si el servidor SMTP cuenta con todos los datos necesarios.
        /// </summary>
        bool IsConfigured { get; }

        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
