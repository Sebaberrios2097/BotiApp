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

    /* ── Records ──────────────────────────────────────────────────────────── */
    public record CrearEmpleadoRequest(string Nombres, string Apellido1, string? Apellido2, int Rut, string? Fono, string? Correo);
    public record CrearUsuarioRequest(int IdEmpleado, int IdTipoUsuario);
    public record ToggleEstadoRequest(int IdUsuario);
}
