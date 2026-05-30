using BotiApp.Hubs;
using BotiApp.Middleware;
using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddInfraestructura(builder.Configuration);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Login/Login";
        options.LogoutPath       = "/Login/Logout";
        options.AccessDeniedPath = "/Home/AccesoDenegado";
        options.ExpireTimeSpan   = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "BotiAppSession";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.UseMiddleware<SetupCheckMiddleware>();

// ── Las rutas de área SIEMPRE antes que la ruta por defecto ───────────────
app.MapControllerRoute(
    name: "setup",
    pattern: "Setup/{action=Index}/{id?}",
    defaults: new { controller = "Setup" });

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
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.MapHub<BoletaHub>("/hubs/boleta");

app.Run();
