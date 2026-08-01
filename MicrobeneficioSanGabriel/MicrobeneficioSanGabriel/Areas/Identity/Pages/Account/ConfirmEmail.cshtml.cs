using MicrobeneficioSanGabriel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;

namespace MicrobeneficioSanGabriel.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public bool ConfirmacionExitosa { get; private set; }
        public string MensajeEstado { get; private set; } = string.Empty;
        public string? Email { get; private set; }
        public string? ReturnUrl { get; private set; }

        public async Task OnGetAsync(
            string? userId,
            string? code,
            string? email = null,
            string? returnUrl = null)
        {
            // Algunos clientes de correo pueden conservar "&amp;" dentro del enlace.
            // Se normaliza la consulta para recuperar correctamente todos los parámetros.
            userId = ObtenerParametroConsulta("userId", userId);
            code = ObtenerParametroConsulta("code", code);
            email = ObtenerParametroConsulta("email", email);
            returnUrl = ObtenerParametroConsulta("returnUrl", returnUrl);

            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(code))
            {
                MensajeEstado = "El enlace de confirmación está incompleto. Solicitá un nuevo correo de verificación.";
                return;
            }

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code.Trim()));
            }
            catch (FormatException)
            {
                MensajeEstado = "El enlace de confirmación no es válido o está dañado. Solicitá un nuevo correo de verificación.";
                return;
            }

            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                user = await _userManager.FindByIdAsync(userId.Trim());
            }

            if (user == null && !string.IsNullOrWhiteSpace(Email))
            {
                user = await _userManager.FindByEmailAsync(Email);
            }

            // Respaldo seguro: si el cliente de correo alteró el identificador o el correo,
            // se busca la cuenta pendiente cuyo token de confirmación sea realmente válido.
            if (user == null)
            {
                user = await BuscarUsuarioPorTokenAsync(token);
            }

            if (user == null)
            {
                MensajeEstado = "No fue posible localizar la cuenta asociada a este enlace. Solicitá un nuevo correo de verificación desde el inicio de sesión.";
                return;
            }

            Email ??= await _userManager.GetEmailAsync(user);

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                ConfirmacionExitosa = true;
                MensajeEstado = "El correo ya estaba confirmado. La cuenta está lista para iniciar sesión.";
                return;
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded || await _userManager.IsEmailConfirmedAsync(user))
            {
                ConfirmacionExitosa = true;
                MensajeEstado = "Tu correo electrónico fue confirmado correctamente. La cuenta ya está lista para ingresar al sistema.";
                return;
            }

            MensajeEstado = "No pudimos validar este enlace. Puede haber vencido o haber sido utilizado anteriormente. Solicitá un nuevo correo de verificación.";
        }

        private string? ObtenerParametroConsulta(string nombre, string? valorEnlazado)
        {
            var consultaCruda = Request.QueryString.Value;

            if (!string.IsNullOrWhiteSpace(consultaCruda))
            {
                // Decodifica entidades HTML como &amp; y vuelve a analizar la consulta.
                var consultaNormalizada = WebUtility.HtmlDecode(consultaCruda)
                    .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);

                var parametros = QueryHelpers.ParseQuery(consultaNormalizada);

                if (parametros.TryGetValue(nombre, out var valor) &&
                    !string.IsNullOrWhiteSpace(valor.FirstOrDefault()))
                {
                    return valor.FirstOrDefault();
                }

                // Compatibilidad con claves recibidas como "amp;code", "amp;email", etc.
                foreach (var parametro in parametros)
                {
                    if (parametro.Key.EndsWith(nombre, StringComparison.OrdinalIgnoreCase))
                    {
                        var candidato = parametro.Value.FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(candidato))
                        {
                            return candidato;
                        }
                    }
                }
            }

            return string.IsNullOrWhiteSpace(valorEnlazado)
                ? null
                : valorEnlazado.Trim();
        }

        private async Task<ApplicationUser?> BuscarUsuarioPorTokenAsync(string token)
        {
            var usuariosPendientes = await _userManager.Users
                .Where(usuario => !usuario.EmailConfirmed)
                .AsNoTracking()
                .ToListAsync();

            foreach (var candidato in usuariosPendientes)
            {
                var tokenValido = await _userManager.VerifyUserTokenAsync(
                    candidato,
                    _userManager.Options.Tokens.EmailConfirmationTokenProvider,
                    UserManager<ApplicationUser>.ConfirmEmailTokenPurpose,
                    token);

                if (tokenValido)
                {
                    return candidato;
                }
            }

            return null;
        }
    }
}
