using System.Net;
using System.Net.Mail;
using System.Text;

namespace MicrobeneficioSanGabriel.Services
{
    /// <summary>
    /// Servicio de correo SMTP real. No utiliza confirmaciones locales ni omite el envío.
    /// Las credenciales se leen desde la configuración de ASP.NET Core, preferiblemente
    /// mediante User Secrets durante el desarrollo.
    /// </summary>
    public sealed class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration config,
            ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public bool IsConfigured
        {
            get
            {
                var enabled = _config.GetValue<bool>("EmailSettings:Enabled");
                var mail = _config["EmailSettings:Mail"]?.Trim();
                var password = _config["EmailSettings:Password"];
                var host = _config["EmailSettings:Host"]?.Trim();
                var port = _config.GetValue<int?>("EmailSettings:Port");

                return enabled
                    && IsValidEmail(mail)
                    && !string.IsNullOrWhiteSpace(password)
                    && !string.IsNullOrWhiteSpace(host)
                    && port is > 0 and <= 65535;
            }
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("El correo destinatario es obligatorio.", nameof(email));
            }

            if (!IsValidEmail(email))
            {
                throw new ArgumentException("El correo destinatario no tiene un formato válido.", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException("El asunto es obligatorio.", nameof(subject));
            }

            ArgumentNullException.ThrowIfNull(htmlMessage);

            if (!IsConfigured)
            {
                _logger.LogWarning(
                    "EmailSettings está incompleto. Configure Mail, Password, Host y Port mediante User Secrets.");
                throw new InvalidOperationException(
                    "El servicio SMTP no está configurado. Ejecute CONFIGURAR_CORREO_GMAIL.bat antes de registrar o reenviar confirmaciones.");
            }

            var senderEmail = _config["EmailSettings:Mail"]!.Trim();
            var displayName = _config["EmailSettings:DisplayName"]?.Trim();
            var host = _config["EmailSettings:Host"]!.Trim();
            var port = _config.GetValue<int>("EmailSettings:Port");
            var enableSsl = _config.GetValue("EmailSettings:EnableSsl", true);
            var password = NormalizePassword(
                _config["EmailSettings:Password"]!,
                host);

            using var client = new SmtpClient(host, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(
                    senderEmail,
                    string.IsNullOrWhiteSpace(displayName)
                        ? "Microbeneficio San Gabriel"
                        : displayName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            message.To.Add(new MailAddress(email.Trim()));

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation(
                    "Correo enviado correctamente a {Email} mediante {Host}:{Port}.",
                    email,
                    host,
                    port);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(
                    ex,
                    "Error SMTP al enviar a {Email}. Estado: {StatusCode}. Servidor: {Host}:{Port}.",
                    email,
                    ex.StatusCode,
                    host,
                    port);
                throw;
            }
        }

        private static string NormalizePassword(string password, string host)
        {
            // Google muestra las contraseñas de aplicación en grupos separados por espacios.
            // SMTP necesita los 16 caracteres continuos.
            if (host.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                return string.Concat(password.Where(character => !char.IsWhiteSpace(character)));
            }

            return password;
        }

        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                _ = new MailAddress(email.Trim());
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
