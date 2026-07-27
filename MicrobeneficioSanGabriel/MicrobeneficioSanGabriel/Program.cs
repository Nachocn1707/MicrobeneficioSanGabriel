using MicrobeneficioSanGabriel.Data;
using MicrobeneficioSanGabriel.Infrastructure;
using MicrobeneficioSanGabriel.Models;
using MicrobeneficioSanGabriel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings:DefaultConnection mediante appsettings.Development.json, User Secrets o variables de entorno.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews(options =>
{
    // Acepta 1.50 y 1,50 en todos los campos decimal del sistema.
    options.ModelBinderProviders.Insert(0, new FlexibleDecimalModelBinderProvider());
});

// El pedido se mantiene temporalmente en sesión hasta que el cliente confirme
// el método de pago. De esta forma no se inserta en la base de datos antes de tiempo.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddScoped<IAnalisisInventarioIAService, AnalisisInventarioIAService>();
builder.Services.AddScoped<IAlertasSistemaService, AlertasSistemaService>();
builder.Services.AddScoped<IReporteIAService, ReporteIAService>();
builder.Services.AddScoped<IPedidoInventarioService, PedidoInventarioService>();

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

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseStaticFiles();

app.UseRouting();

// Debe ejecutarse antes de autenticación/autorización para que los controladores
// puedan leer y limpiar el pedido temporal.
app.UseSession();

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

    // La aplicación no debe iniciar con un esquema incompleto: las columnas de
    // propiedad, concurrencia e inventario son necesarias para operar de forma segura.
    await dbContext.Database.MigrateAsync();
    await DatabaseCompatibilityHelper.EnsureLatestSchemaAsync(dbContext);
    await DbInitializer.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

var wwwrootPath = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
RotativaConfiguration.Setup(wwwrootPath, "Rotativa");

app.Run();