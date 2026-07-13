using System.Net;
using System.Net.Mail;

namespace MicrobeneficioSanGabriel.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var mail = _config["EmailSettings:Mail"]?.Trim();
            var displayName = _config["EmailSettings:DisplayName"];
            var password = _config["EmailSettings:Password"];
            var host = _config["EmailSettings:Host"]?.Trim();
            var port = _config.GetValue<int?>("EmailSettings:Port") ?? 587;

            if (string.IsNullOrWhiteSpace(mail) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(host))
            {
                _logger.LogError("El servicio de correo no está configurado. Use User Secrets o variables de entorno para EmailSettings.");
                throw new InvalidOperationException("El servicio de correo no está configurado.");
            }

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(mail, password),
                EnableSsl = true
            };

            using var message = new MailMessage
            {
                From = new MailAddress(mail, displayName ?? "Microbeneficio San Gabriel"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(email);
            await client.SendMailAsync(message);
        }
    }
}
