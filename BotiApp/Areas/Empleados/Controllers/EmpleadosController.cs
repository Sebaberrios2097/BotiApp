using BotiApp.Helpers;
using Infraestructura.Context;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Infraestructura.Repositories.Sp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BotiApp.Areas.Empleados.Controllers;

[Area("Empleados")]
[Authorize(Policy = "SoloAdmin")]
public class EmpleadosController(
    IEmpEmpleadoRepository empleadoRepo,
    IEmpUsuariosRepository usuarioRepo,
    IBotiAppStoredProcedures sp,
    BotiAppContext db) : Controller
{
    // ── GET /Empleados/Empleados/Index ────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        if (!ClaimHelper.EsAdmin(User))
            return Forbid();

        var empleados   = await empleadoRepo.GetAllWithUsuario();
        ViewBag.Tipos   = await db.EmpTiposUsuario.OrderBy(t => t.IdTipoUsuario).ToListAsync();
        return View(empleados);
    }

    // ── POST /Empleados/Empleados/CrearEmpleadoAjax ──────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEmpleadoAjax([FromBody] CrearEmpleadoRequest req)
    {
        if (!ClaimHelper.EsAdmin(User))
            return Json(new { ok = false, mensaje = "Sin permiso." });

        var empleado = new Infraestructura.Entities.BotiApp.EmpEmpleado
        {
            NombresEmpleado = req.Nombres.Trim(),
            Apellido1       = req.Apellido1.Trim(),
            Apellido2       = string.IsNullOrWhiteSpace(req.Apellido2) ? null : req.Apellido2.Trim(),
            Rut             = req.Rut,
            Fono            = string.IsNullOrWhiteSpace(req.Fono)    ? null : req.Fono.Trim(),
            Correo          = string.IsNullOrWhiteSpace(req.Correo)  ? null : req.Correo.Trim(),
            FechaIngreso    = DateTime.Now
        };

        var creado = await empleadoRepo.Create(empleado);

        var rut    = creado.Rut.ToString();
        var rutFmt = rut.Length >= 2 ? rut[..^1] + "-" + rut[^1..] : rut;
        var nombre = $"{creado.NombresEmpleado} {creado.Apellido1}{(creado.Apellido2 != null ? " " + creado.Apellido2 : "")}";

        return Json(new
        {
            ok         = true,
            mensaje    = "Empleado creado correctamente.",
            idEmpleado = creado.IdEmpleado,
            nombre,
            rut        = rutFmt,
            rutRaw     = creado.Rut,
            fono       = creado.Fono,
            correo     = creado.Correo,
            fechaIngreso = creado.FechaIngreso.ToString("dd/MM/yyyy")
        });
    }

    // ── POST /Empleados/Empleados/CrearUsuarioAjax ────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearUsuarioAjax([FromBody] CrearUsuarioRequest req)
    {
        if (!ClaimHelper.EsAdmin(User))
            return Json(new { ok = false, mensaje = "Sin permiso." });

        var empleado = await empleadoRepo.GetById(req.IdEmpleado);
        if (empleado is null)
            return Json(new { ok = false, mensaje = "Empleado no encontrado." });

        var resultado = await sp.SpEmpCreaUsuarioEmpleado(empleado.Rut, req.IdTipoUsuario);

        // Recargar empleado con usuario recién creado
        var empleados = await empleadoRepo.GetAllWithUsuario();
        var actualizado = empleados.FirstOrDefault(e => e.IdEmpleado == req.IdEmpleado);

        var usuario = actualizado?.EmpUsuario;

        return Json(new
        {
            ok      = true,
            mensaje = resultado,
            usuario = usuario is null ? null : new
            {
                idUsuario       = usuario.IdUsuario,
                nombreUsuario   = usuario.NombreUsuario,
                tipoUsuario     = usuario.IdTipoUsuarioNavigation?.NombreTipoUsuario ?? "—",
                estado          = usuario.Estado
            }
        });
    }

    // ── POST /Empleados/Empleados/ToggleEstadoAjax ────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEstadoAjax([FromBody] ToggleEstadoRequest req)
    {
        if (!ClaimHelper.EsAdmin(User))
            return Json(new { ok = false, mensaje = "Sin permiso." });

        var nuevoEstado = await usuarioRepo.ToggleEstado(req.IdUsuario);
        if (nuevoEstado is null)
            return Json(new { ok = false, mensaje = "Usuario no encontrado." });

        return Json(new { ok = true, estado = nuevoEstado });
    }

    // ── POST /Empleados/Empleados/EditarEmpleadoAjax ──────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarEmpleadoAjax([FromBody] EditarEmpleadoRequest req)
    {
        if (!ClaimHelper.EsAdmin(User))
            return Json(new { ok = false, mensaje = "Sin permiso." });

        var actualizado = await empleadoRepo.Update(req.IdEmpleado, req.Nombres, req.Apellido1, req.Apellido2, req.Fono, req.Correo, req.Rut);
        if (actualizado is null)
            return Json(new { ok = false, mensaje = "Empleado no encontrado." });

        var dv     = CalcDv(actualizado.Rut);
        var rutFmt = $"{actualizado.Rut}-{dv}";
        var nombre = $"{actualizado.NombresEmpleado} {actualizado.Apellido1}{(actualizado.Apellido2 != null ? " " + actualizado.Apellido2 : "")}";

        return Json(new
        {
            ok         = true,
            mensaje    = "Datos del empleado actualizados.",
            idEmpleado = actualizado.IdEmpleado,
            nombre,
            rut        = actualizado.Rut,
            rutFmt,
            fono       = actualizado.Fono,
            correo     = actualizado.Correo
        });
    }

    // ── POST /Empleados/Empleados/ResetPasswordAjax ───────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordAjax([FromBody] ResetPasswordRequest req)
    {
        if (!ClaimHelper.EsAdmin(User))
            return Json(new { ok = false, mensaje = "Sin permiso." });

        if (string.IsNullOrWhiteSpace(req.NuevaClave) || req.NuevaClave.Length < 6)
            return Json(new { ok = false, mensaje = "La contraseña debe tener al menos 6 caracteres." });

        var resultado = await sp.SpEmpResetContrasena(req.IdUsuario, req.NuevaClave.Trim());
        if (resultado < 1)
            return Json(new { ok = false, mensaje = resultado == -1 ? "Usuario no encontrado." : "No se pudo restablecer la contraseña." });

        return Json(new { ok = true, mensaje = "Contraseña actualizada correctamente." });
    }

    /* ── Helper: calcula dígito verificador RUT chileno ──────────────────── */
    private static string CalcDv(int rut)
    {
        int suma = 0, mul = 2, n = rut;
        while (n > 0) { suma += (n % 10) * mul; n /= 10; mul = mul == 7 ? 2 : mul + 1; }
        int dv = 11 - (suma % 11);
        return dv == 11 ? "0" : dv == 10 ? "K" : dv.ToString();
    }

    /* ── Records ──────────────────────────────────────────────────────────── */
    public record CrearEmpleadoRequest(string Nombres, string Apellido1, string? Apellido2, int Rut, string? Fono, string? Correo);
    public record CrearUsuarioRequest(int IdEmpleado, int IdTipoUsuario);
    public record ToggleEstadoRequest(int IdUsuario);
    public record EditarEmpleadoRequest(int IdEmpleado, string Nombres, string Apellido1, string? Apellido2, string? Fono, string? Correo, int? Rut);
    public record ResetPasswordRequest(int IdUsuario, string NuevaClave);
}
