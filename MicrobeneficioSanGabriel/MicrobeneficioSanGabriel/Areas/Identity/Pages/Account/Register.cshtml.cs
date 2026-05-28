// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

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
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailService _emailService;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
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
            [Required(ErrorMessage = "El correo es obligatorio.")]
            [EmailAddress(ErrorMessage = "Debe ingresar un correo válido.")]
            [Display(Name = "Correo electrónico")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria.")]
            [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

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

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(
                    user,
                    Input.Email,
                    CancellationToken.None);

                await _emailStore.SetEmailAsync(
                    user,
                    Input.Email,
                    CancellationToken.None);

                var result = await _userManager.CreateAsync(
                    user,
                    Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario registrado correctamente.");

                    if (!await _roleManager.RoleExistsAsync("Cliente"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Cliente"));
                    }

                    await _userManager.AddToRoleAsync(user, "Cliente");

                    var userId = await _userManager.GetUserIdAsync(user);

                    var code = await _userManager
                        .GenerateEmailConfirmationTokenAsync(user);

                    code = WebEncoders.Base64UrlEncode(
                        Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new
                        {
                            area = "Identity",
                            userId = userId,
                            code = code,
                            returnUrl = returnUrl
                        },
                        protocol: Request.Scheme);

                    await _emailService.SendEmailAsync(
                        Input.Email,
                        "Confirmación de cuenta",
                        EmailTemplate.BaseTemplate(
                        "Confirmación",
                        "Gracias por registrarte. Confirmá tu correo.",
                        "Confirmar correo",
                        HtmlEncoder.Default.Encode(callbackUrl)
                                                       ));

                    TempData["CorreoConfirmacion"] = true;
                    return RedirectToPage("./RegisterConfirmation");
                }

                bool emailDuplicadoMostrado = false;

                foreach (var error in result.Errors)
                {
                    string mensaje = error.Description;

                    if (mensaje.Contains("is already taken"))
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

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"No se puede crear una instancia de '{nameof(IdentityUser)}'.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("La interfaz predeterminada requiere soporte para correo electrónico.");
            }

            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }


}
