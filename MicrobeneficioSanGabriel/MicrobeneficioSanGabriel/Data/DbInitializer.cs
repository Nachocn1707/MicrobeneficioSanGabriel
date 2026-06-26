using MicrobeneficioSanGabriel.Models;
using Microsoft.AspNetCore.Identity;

namespace MicrobeneficioSanGabriel.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Administrador", "Operador", "Vendedor", "Cliente" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            string adminEmail = "admin@microbeneficio.com";
            string adminPassword = "Admin123*";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    Nombre = "Administrador",
                    Apellidos = "General",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    PhoneNumber = "8888-8888"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, "Administrador"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
                }
            }
        }
    }
}