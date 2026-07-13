using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Services;
using MicrobeneficioSanGabriel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuariosController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
                    Nombre = usuario.Nombre,
                    Apellidos = usuario.Apellidos,
                    Email = usuario.Email ?? "",
                    UserName = usuario.UserName ?? "",
                    PhoneNumber = SoloDigitos(usuario.PhoneNumber),
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
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Email = usuario.Email ?? "",
                UserName = usuario.UserName ?? "",
                PhoneNumber = SoloDigitos(usuario.PhoneNumber),
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
            model.PhoneNumber = SoloDigitos(model.PhoneNumber);
            ModelState.Remove(nameof(model.PhoneNumber));
            if (model.PhoneNumber.Length != 8)
            {
                ModelState.AddModelError(nameof(model.PhoneNumber),
                    "El teléfono debe contener exactamente 8 dígitos.");
            }

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

            var usuario = new ApplicationUser
            {
                Nombre = model.Nombre,
                Apellidos = model.Apellidos,
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
                    var resultadoRol = await _userManager.AddToRoleAsync(usuario, model.Rol);
                    if (!resultadoRol.Succeeded)
                    {
                        await _userManager.DeleteAsync(usuario);
                        foreach (var error in resultadoRol.Errors)
                        {
                            ModelState.AddModelError(nameof(model.Rol), error.Description);
                        }
                        return View(model);
                    }
                }

                await AuditoriaHelper.RegistrarAsync(_context, User, "Usuarios", "Crear", null,
                    $"Se registró el usuario {usuario.NombreCompleto} ({usuario.Email}) con rol {model.Rol}.");
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
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Email = usuario.Email ?? "",
                PhoneNumber = SoloDigitos(usuario.PhoneNumber),
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
            model.PhoneNumber = SoloDigitos(model.PhoneNumber);
            ModelState.Remove(nameof(model.PhoneNumber));
            if (model.PhoneNumber.Length != 8)
            {
                ModelState.AddModelError(nameof(model.PhoneNumber),
                    "El teléfono debe contener exactamente 8 dígitos.");
            }

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

            var rolesActuales = await _userManager.GetRolesAsync(usuario);
            var esAdministradorActual = rolesActuales.Contains("Administrador");
            var dejaraDeSerAdministrador = esAdministradorActual && model.Rol != "Administrador";

            if (dejaraDeSerAdministrador && await ContarAdministradoresAsync() <= 1)
            {
                ModelState.AddModelError(nameof(model.Rol),
                    "No se puede retirar el rol al último administrador del sistema.");
                return View(model);
            }

            usuario.Nombre = model.Nombre;
            usuario.Apellidos = model.Apellidos;
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

            var seAgregoRolNuevo = false;
            if (!string.IsNullOrWhiteSpace(model.Rol) && !rolesActuales.Contains(model.Rol))
            {
                var agregarRol = await _userManager.AddToRoleAsync(usuario, model.Rol);
                if (!agregarRol.Succeeded)
                {
                    foreach (var error in agregarRol.Errors)
                    {
                        ModelState.AddModelError(nameof(model.Rol), error.Description);
                    }
                    return View(model);
                }

                seAgregoRolNuevo = true;
            }

            var rolesARemover = rolesActuales.Where(r => r != model.Rol).ToList();
            if (rolesARemover.Count > 0)
            {
                var removerRoles = await _userManager.RemoveFromRolesAsync(usuario, rolesARemover);
                if (!removerRoles.Succeeded)
                {
                    if (seAgregoRolNuevo && !string.IsNullOrWhiteSpace(model.Rol))
                    {
                        await _userManager.RemoveFromRoleAsync(usuario, model.Rol);
                    }

                    foreach (var error in removerRoles.Errors)
                    {
                        ModelState.AddModelError(nameof(model.Rol), error.Description);
                    }
                    return View(model);
                }
            }

            await AuditoriaHelper.RegistrarAsync(_context, User, "Usuarios", "Editar", null,
                $"Se actualizó el usuario {usuario.NombreCompleto} ({usuario.Email}) con rol {model.Rol}.");
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
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Email = usuario.Email ?? "",
                UserName = usuario.UserName ?? "",
                PhoneNumber = SoloDigitos(usuario.PhoneNumber),
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

            if (await _userManager.IsInRoleAsync(usuario, "Administrador") &&
                await ContarAdministradoresAsync() <= 1)
            {
                TempData["Error"] = "No se puede eliminar el último administrador del sistema.";
                return RedirectToAction(nameof(Index));
            }

            if (await _context.Pedidos.AnyAsync(p => p.ClienteId == usuario.Id))
            {
                TempData["Error"] = "No se puede eliminar el usuario porque tiene pedidos asociados y debe conservarse el historial.";
                return RedirectToAction(nameof(Index));
            }

            var resultado = await _userManager.DeleteAsync(usuario);

            if (resultado.Succeeded)
            {
                await AuditoriaHelper.RegistrarAsync(_context, User, "Usuarios", "Eliminar", null,
                    $"Se eliminó el usuario {usuario.NombreCompleto} ({usuario.Email}).");
                TempData["Success"] = "Usuario eliminado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
            {
                TempData["Error"] = error.Description;
            }

            return RedirectToAction(nameof(Index));
        }


        private static string SoloDigitos(string? valor)
        {
            return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        }

        private async Task<int> ContarAdministradoresAsync()
        {
            var rol = await _roleManager.FindByNameAsync("Administrador");
            if (rol == null) return 0;
            var usuarios = await _userManager.GetUsersInRoleAsync("Administrador");
            return usuarios.Count;
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

        [HttpGet]
        public async Task<JsonResult> CheckEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            return Json(new
            {
                exists = user != null
            });
        }
    }
}