#nullable disable

using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;

namespace MicrobeneficioSanGabriel.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<RegisterConfirmationModel> _logger;

        public RegisterConfirmationModel(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<RegisterConfirmationModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        public string Email { get; set; }
        public string EmailMasked { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string CorreoReenviado { get; set; }

        [TempData]
        public string ErrorEnvioCorreo { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToPage("./Register");
            }

            returnUrl ??= Url.Content("~/");
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return RedirectToPage("./Register");
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["RegistroExitoso"] = "El correo ya está confirmado. Puede iniciar sesión.";
                return RedirectToPage("./Login", new { returnUrl });
            }

            CargarVista(email, returnUrl);
            return Page();
        }

        public async Task<IActionResult> OnPostResendAsync(string email, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToPage("./Register");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Respuesta genérica para no revelar cuentas existentes.
                CorreoReenviado = "Si la cuenta existe y está pendiente, se enviará un nuevo enlace.";
                return RedirectToPage(new { email, returnUrl });
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["RegistroExitoso"] = "El correo ya está confirmado. Puede iniciar sesión.";
                return RedirectToPage("./Login", new { returnUrl });
            }

            if (!_emailService.IsConfigured)
            {
                ErrorEnvioCorreo =
                    "El servicio de correo no está configurado. Ejecute CONFIGURAR_CORREO_GMAIL.bat y vuelva a intentar.";
                return RedirectToPage(new { email, returnUrl });
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code, email = user.Email, returnUrl },
                protocol: Request.Scheme)!;

            try
            {
                await _emailService.SendEmailAsync(
                    email,
                    "Reenvío de confirmación - Microbeneficio San Gabriel",
                    EmailTemplate.BaseTemplate(
                        "Confirmación de cuenta",
                        "Recibimos una solicitud para reenviar el enlace. Confirmá tu correo para habilitar el inicio de sesión.",
                        "Confirmar correo",
                        callbackUrl));

                CorreoReenviado = "Enviamos un nuevo enlace de confirmación. Revise también Spam o Correo no deseado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo reenviar la confirmación a {Email}.", email);
                ErrorEnvioCorreo =
                    "No fue posible enviar el correo en este momento. Verifique la configuración SMTP e inténtelo nuevamente.";
            }

            return RedirectToPage(new { email, returnUrl });
        }

        private void CargarVista(string email, string returnUrl)
        {
            Email = email;
            EmailMasked = OcultarCorreo(email);
            ReturnUrl = returnUrl;
        }

        private static string OcultarCorreo(string email)
        {
            var partes = email.Split('@', 2);
            if (partes.Length != 2 || partes[0].Length <= 2)
            {
                return email;
            }

            var usuario = partes[0];
            return $"{usuario[0]}{new string('*', Math.Max(2, usuario.Length - 2))}{usuario[^1]}@{partes[1]}";
        }
    }
}
