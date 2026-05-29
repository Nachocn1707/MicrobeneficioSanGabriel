using MicrobeneficioSanGabriel.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.NetworkInformation;
using System.Text;

namespace MicrobeneficioSanGabriel.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(
            string userId,
            string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound(
                    $"No se pudo cargar el usuario.");
            }

            code = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(code));

            var result = await _userManager
                .ConfirmEmailAsync(user, code);

            if (!result.Succeeded)
            {
                return BadRequest(
                    "Error al confirmar correo.");
            }
            return Page();
        }
    }
}
