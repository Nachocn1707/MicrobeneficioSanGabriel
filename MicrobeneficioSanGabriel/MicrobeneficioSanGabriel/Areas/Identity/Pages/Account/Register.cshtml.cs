// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using MicrobeneficioSanGabriel.Infrastructure;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace MicrobeneficioSanGabriel.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailService _emailService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [NotWhiteSpace(ErrorMessage = "El nombre es obligatorio.")]
            [Display(Name = "Nombre")]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "Los apellidos son obligatorios.")]
            [NotWhiteSpace(ErrorMessage = "Los apellidos son obligatorios.")]
            [Display(Name = "Apellidos")]
            public string Apellidos { get; set; } = string.Empty;

            [Required(ErrorMessage = "El teléfono es obligatorio.")]
            [RegularExpression(@"^\d{8}$", ErrorMessage = "El teléfono debe contener exactamente 8 dígitos, por ejemplo 88888888.")]
            [Display(Name = "Teléfono")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "El correo es obligatorio.")]
            [CompleteEmailAddress]
            [DataType(DataType.EmailAddress)]
            [Display(Name = "Correo electrónico")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria.")]
            [NotWhiteSpace(ErrorMessage = "La contraseña es obligatoria.")]
            [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Debe confirmar la contraseña.")]
            [NotWhiteSpace(ErrorMessage = "Debe confirmar la contraseña.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();

            Input.PhoneNumber = SoloDigitos(Input.PhoneNumber);
            ModelState.Remove("Input.PhoneNumber");
            if (Input.PhoneNumber.Length != 8)
            {
                ModelState.AddModelError("Input.PhoneNumber",
                    "El teléfono debe contener exactamente 8 dígitos.");
            }

            // El registro requiere SMTP real para que la cuenta reciba el enlace obligatorio de confirmación.
            if (ModelState.IsValid && !_emailService.IsConfigured)
            {
                ModelState.AddModelError(string.Empty,
                    "El servicio de correo todavía no está configurado. Ejecute CONFIGURAR_CORREO_GMAIL.bat antes de registrar usuarios.");
                return Page();
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                user.Nombre = Input.Nombre;
                user.Apellidos = Input.Apellidos;
                user.PhoneNumber = Input.PhoneNumber;
                user.EmailConfirmed = false;

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario registrado correctamente y pendiente de confirmar correo.");

                    if (!await _roleManager.RoleExistsAsync("Cliente"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Cliente"));
                    }

                    var roleResult = await _userManager.AddToRoleAsync(user, "Cliente");
                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        await _userManager.DeleteAsync(user);
                        return Page();
                    }

                    var callbackUrl = await CrearUrlConfirmacionAsync(user, returnUrl);

                    try
                    {
                        await _emailService.SendEmailAsync(
                            Input.Email,
                            "Confirme su cuenta - Microbeneficio San Gabriel",
                            EmailTemplate.BaseTemplate(
                                "Confirmación de cuenta",
                                $"Hola {HtmlEncoder.Default.Encode(Input.Nombre)}, gracias por registrarte. Para activar tu cuenta y poder iniciar sesión, confirmá tu correo electrónico.",
                                "Confirmar correo",
                                callbackUrl));

                        TempData["RegistroExitoso"] =
                            "Cuenta creada. Enviamos un enlace de confirmación a su correo.";
                    }
                    catch (Exception ex)
                    {
                        // La cuenta permanece sin confirmar. El usuario puede reenviar
                        // el enlace desde la pantalla de confirmación.
                        _logger.LogError(ex,
                            "No se pudo enviar el correo de confirmación a {Email}.",
                            Input.Email);

                        TempData["ErrorEnvioCorreo"] =
                            "La cuenta fue creada, pero no pudimos enviar el correo en este momento. Revise la configuración SMTP y utilice “Reenviar correo”.";
                    }

                    return RedirectToPage("./RegisterConfirmation", new
                    {
                        email = Input.Email,
                        returnUrl
                    });
                }

                bool emailDuplicadoMostrado = false;

                foreach (var error in result.Errors)
                {
                    string mensaje = error.Description;

                    if (error.Code is "DuplicateUserName" or "DuplicateEmail")
                    {
                        if (emailDuplicadoMostrado)
                        {
                            continue;
                        }

                        mensaje = "El correo ya está registrado.";
                        emailDuplicadoMostrado = true;
                    }

                    ModelState.AddModelError(string.Empty, mensaje);
                }
            }

            return Page();
        }

        private async Task<string> CrearUrlConfirmacionAsync(ApplicationUser user, string returnUrl)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            return Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    userId,
                    code,
                    email = user.Email,
                    returnUrl
                },
                protocol: Request.Scheme)!;
        }

        private static string SoloDigitos(string? valor)
        {
            return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"No se puede crear una instancia de '{nameof(ApplicationUser)}'.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("La interfaz predeterminada requiere soporte para correo electrónico.");
            }

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
