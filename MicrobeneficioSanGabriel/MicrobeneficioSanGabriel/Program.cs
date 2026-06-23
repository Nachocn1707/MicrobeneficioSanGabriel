using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

builder.Services.AddScoped<IAnalisisInventarioIAService, AnalisisInventarioIAService>();
builder.Services.AddScoped<IAlertasSistemaService, AlertasSistemaService>();
builder.Services.AddScoped<IReporteIAService, ReporteIAService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    // Aplica automáticamente las migraciones pendientes antes de crear roles y usuarios.
    // Después valida columnas nuevas críticas para bases de datos existentes.
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("pending model changes", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("cambios pendientes", StringComparison.OrdinalIgnoreCase))
    {
        // En algunas bases locales EF puede detectar cambios pendientes aunque la migración exista.
        // No se detiene el sistema; se ejecuta la verificación de compatibilidad justo abajo.
        Console.WriteLine($"Migraciones pendientes detectadas por EF: {ex.Message}");
    }

    await DatabaseCompatibilityHelper.EnsureLatestSchemaAsync(dbContext);
    await DbInitializer.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

app.Run();