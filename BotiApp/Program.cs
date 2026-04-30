using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddInfraestructura(builder.Configuration);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Login/Login";
        options.LogoutPath       = "/Login/Logout";
        options.AccessDeniedPath = "/Home/AccesoDenegado";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    // Solo administrador
    options.AddPolicy("SoloAdmin",      p => p.RequireClaim("TipoUsuario", "Administrador"));
    // Administrador + Vendedor (Generar venta, Historial)
    options.AddPolicy("AdminOVendedor", p => p.RequireClaim("TipoUsuario", "Administrador", "Vendedor"));
    // Administrador + Cajero (Caja, cobros)
    options.AddPolicy("AdminOCajero",   p => p.RequireClaim("TipoUsuario", "Administrador", "Cajero"));
    // Todos los roles (catálogo de lectura)
    options.AddPolicy("TodosRoles",     p => p.RequireClaim("TipoUsuario", "Administrador", "Vendedor", "Cajero"));
});

var app = builder.Build();

// ── Seed: crear usuario administrador por defecto si no existe ninguno ────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<BotiAppContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var hayAdmin = await db.EmpUsuario.AnyAsync(u => u.IdTipoUsuario == 1);
    if (!hayAdmin)
    {
        var empleadoAdmin = new EmpEmpleado
        {
            NombresEmpleado = "Administrador",
            Apellido1       = "Sistema",
            Rut             = 0,
            FechaIngreso    = DateTime.Now
        };
        db.EmpEmpleado.Add(empleadoAdmin);
        await db.SaveChangesAsync();

        var claveHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.Unicode.GetBytes("admin")));

        db.EmpUsuario.Add(new EmpUsuario
        {
            IdEmpleado     = empleadoAdmin.IdEmpleado,
            IdTipoUsuario  = 1,
            NombreUsuario  = "admin",
            ClaveUsuario   = claveHash,
            Estado         = true
        });
        await db.SaveChangesAsync();

        logger.LogWarning(
            "AVISO DE SEGURIDAD: Se creó el usuario administrador por defecto " +
            "(usuario: admin / clave: admin). Cree un nuevo administrador y " +
            "desactive este usuario a la brevedad.");
    }
}
// ─────────────────────────────────────────────────────────────────────────

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// ── Las rutas de área SIEMPRE antes que la ruta por defecto ───────────────
app.MapAreaControllerRoute(
    name: "areas-ventas",
    areaName: "Ventas",
    pattern: "Ventas/{controller=GenerarVenta}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "areas-compras",
    areaName: "Compras",
    pattern: "Compras/{controller=Proveedores}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "areas-productos",
    areaName: "Productos",
    pattern: "Productos/{controller=Productos}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "areas-empleados",
    areaName: "Empleados",
    pattern: "Empleados/{controller=Empleados}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
