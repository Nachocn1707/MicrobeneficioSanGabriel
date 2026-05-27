using System.Net;
using System.Net.Mail;

namespace MicrobeneficioSanGabriel.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage)
        {
            var mail = _config["EmailSettings:Mail"];
            var displayName = _config["EmailSettings:DisplayName"];
            var password = _config["EmailSettings:Password"];
            var host = _config["EmailSettings:Host"];
            var port = _config.GetValue<int>("EmailSettings:Port");

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(mail, password),
                EnableSsl = true
            };

            if (string.IsNullOrEmpty(mail))
            {
                throw new Exception("EmailSettings:Mail no está configurado.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(mail,displayName ?? "Microbeneficio San Gabriel"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(email);

            await client.SendMailAsync(message);
        }
    }
}
