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
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            string[] roles = { "Administrador", "Operador", "Vendedor", "Cliente" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"No se pudo crear el rol {role}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
            }

            var adminEmail = configuration["AdminSeed:Email"]?.Trim();
            var adminPassword = configuration["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogInformation(
                    "No se creó un administrador inicial. Configure AdminSeed:Email y AdminSeed:Password mediante User Secrets o variables de entorno.");
                return;
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    Nombre = configuration["AdminSeed:Nombre"]?.Trim() ?? "Administrador",
                    Apellidos = configuration["AdminSeed:Apellidos"]?.Trim() ?? "General",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    PhoneNumber = configuration["AdminSeed:Telefono"]?.Trim()
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"No se pudo crear el administrador inicial: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, "Administrador"))
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Administrador");
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"No se pudo asignar el rol Administrador: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
