using MicrobeneficioSanGabriel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuariosController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = _userManager.Users.ToList();
            var lista = new List<UsuarioListadoViewModel>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);

                lista.Add(new UsuarioListadoViewModel
                {
                    Id = usuario.Id,
                    Email = usuario.Email ?? "",
                    UserName = usuario.UserName ?? "",
                    PhoneNumber = usuario.PhoneNumber,
                    Rol = roles.FirstOrDefault() ?? "Sin rol"
                });
            }

            return View(lista);
        }

        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(usuario);

            var model = new UsuarioListadoViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email ?? "",
                UserName = usuario.UserName ?? "",
                PhoneNumber = usuario.PhoneNumber,
                Rol = roles.FirstOrDefault() ?? "Sin rol"
            };

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            var model = new UsuarioCrearViewModel
            {
                Roles = await ObtenerRolesAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCrearViewModel model)
        {
            model.Roles = await ObtenerRolesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existeUsuario = await _userManager.FindByEmailAsync(model.Email);

            if (existeUsuario != null)
            {
                ModelState.AddModelError("Email", "Ya existe un usuario registrado con este correo.");
                return View(model);
            }

            var usuario = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var resultado = await _userManager.CreateAsync(usuario, model.Password);

            if (resultado.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(model.Rol))
                {
                    await _userManager.AddToRoleAsync(usuario, model.Rol);
                }

                TempData["Success"] = "Usuario registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            var rolesUsuario = await _userManager.GetRolesAsync(usuario);

            var model = new UsuarioEditarViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email ?? "",
                PhoneNumber = usuario.PhoneNumber,
                Rol = rolesUsuario.FirstOrDefault() ?? "",
                Roles = await ObtenerRolesAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UsuarioEditarViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            model.Roles = await ObtenerRolesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            var usuarioConCorreo = await _userManager.FindByEmailAsync(model.Email);

            if (usuarioConCorreo != null && usuarioConCorreo.Id != usuario.Id)
            {
                ModelState.AddModelError("Email", "Ya existe otro usuario con este correo.");
                return View(model);
            }

            usuario.Email = model.Email;
            usuario.UserName = model.Email;
            usuario.PhoneNumber = model.PhoneNumber;

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            var rolesActuales = await _userManager.GetRolesAsync(usuario);
            await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);

            if (!string.IsNullOrWhiteSpace(model.Rol))
            {
                await _userManager.AddToRoleAsync(usuario, model.Rol);
            }

            TempData["Success"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(usuario);

            var model = new UsuarioListadoViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email ?? "",
                UserName = usuario.UserName ?? "",
                PhoneNumber = usuario.PhoneNumber,
                Rol = roles.FirstOrDefault() ?? "Sin rol"
            };

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            var usuarioActual = await _userManager.GetUserAsync(User);

            if (usuarioActual != null && usuarioActual.Id == usuario.Id)
            {
                TempData["Error"] = "No puede eliminar su propio usuario.";
                return RedirectToAction(nameof(Index));
            }

            var resultado = await _userManager.DeleteAsync(usuario);

            if (resultado.Succeeded)
            {
                TempData["Success"] = "Usuario eliminado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
            {
                TempData["Error"] = error.Description;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> ObtenerRolesAsync()
        {
            return await Task.FromResult(
                _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => new SelectListItem
                    {
                        Value = r.Name,
                        Text = r.Name
                    })
                    .ToList()
            );
        }
    }
}