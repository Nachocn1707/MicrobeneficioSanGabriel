// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using MicrobeneficioSanGabriel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace MicrobeneficioSanGabriel.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await _emailService.SendEmailAsync(
        Input.Email,
        "Recuperación de contraseña",
        $@"

        <div style='
    font-family:Poppins,Arial,sans-serif;
    max-width:600px;
    margin:auto;
    padding:40px;
    background:#f8f5e9;
    border-radius:18px;
    border:1px solid #e8dcc2;
    box-shadow:0 4px 18px rgba(0,0,0,0.05);'>
    <div style='text-align:center;
                margin-bottom:25px;'>
        <h1 style='
            color:#5c3317;
            margin-bottom:8px;
            font-size:32px;
            font-weight:700;'>

            Microbeneficio San Gabriel
        </h1>
        <p style='
            color:#8b7355;
            font-size:15px;
            margin:0;'>

            Sistema de recuperación de cuenta
        </p>
    </div>
    <div style='
        background:white;
        border-radius:14px;
        padding:30px;
        border:1px solid #efe4cc;'>
        
        <p style='
            font-size:15px;
            line-height:1.8;
            color:#555;'>

            Recibimos una solicitud para restablecer la contraseña de tu cuenta.
        </p>
        <p style='
            font-size:15px;
            line-height:1.8;
            color:#555;'>

            Para continuar, clic en el siguiente botón:
        </p>
        <div style='
            text-align:center;
            margin:35px 0;'>
            <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'
               style='
                background:#5c3317;
                color:white;
                padding:15px 32px;
                text-decoration:none;
                border-radius:12px;
                font-weight:600;
                font-size:15px;
                display:inline-block;
                box-shadow:0 4px 12px rgba(92,51,23,0.25);'>

                Restablecer contraseña
            </a>
        </div>
        <p style='
            font-size:14px;
            color:#777;
            line-height:1.7;
            margin-bottom:0;'>

            Si no solicitaste este cambio, podés ignorar este correo.
        </p>
    </div>
    <div style='margin-top:25px;'>

        <img src='https://raw.githubusercontent.com/Kevinscr2345/Interfaz_Proyecto/main/EmailFooter.png'
             style='
                width:75%;
                display:block;
                margin:auto;
                border-radius:12px;'>
    </div>
</div>");

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
