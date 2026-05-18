using BotiApp.Helpers;
using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BotiApp.Areas.Ventas.Controllers;

[Area("Ventas")]
[Authorize(Policy = "AdminOCajero")]
public class FiadoController(IFiadoRepository fiadoRepository, BotiAppContext context) : Controller
{
    // ── GET /Ventas/Fiado ─────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var clientes = await fiadoRepository.ObtenerClientesAsync();

        // Calcular deuda por cliente desde boletas fiadas (estado 4)
        var vm = new FiadoIndexViewModel
        {
            Clientes = clientes.ToList(),
            TotalGlobalAdeudado = await fiadoRepository.ObtenerTotalGlobalAdeudadoAsync(),
            CantClientesConDeuda = await fiadoRepository.ObtenerCantidadClientesConDeudaAsync()
        };
        return View(vm);
    }

    // ── GET /Ventas/Fiado/Cliente/5 ───────────────────────────────────────────
    public async Task<IActionResult> Cliente(int id)
    {
        var cliente = await fiadoRepository.ObtenerClientePorIdAsync(id);
        if (cliente == null) return NotFound();

        var boletasFiadas = await fiadoRepository.ObtenerBoletasFiadasPorClienteAsync(id);
        var abonos        = await fiadoRepository.ObtenerAbonosPorClienteAsync(id);
        var metodosPago   = await context.VenMetodosPago.AsNoTracking().ToListAsync();

        var deudaTotal = boletasFiadas.Sum(b => b.MontoTotal);

        var vm = new FiadoClienteViewModel
        {
            Cliente       = cliente,
            BoletasFiadas = boletasFiadas.ToList(),
            Abonos        = abonos.ToList(),
            DeudaTotal    = deudaTotal,
            MetodosPago   = metodosPago
        };
        return View(vm);
    }

    // ── POST: registrar abono ─────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAbonoAjax([FromBody] RegistrarAbonoRequest request)
    {
        if (request.Monto <= 0)
            return Json(new { ok = false, mensaje = "El monto debe ser mayor a 0." });

        if (request.IdMetodoPago <= 0)
            return Json(new { ok = false, mensaje = "Selecciona un método de pago." });

        var idUsuario = ClaimHelper.GetIdUsuario(User);
        if (idUsuario == 0)
            return Json(new { ok = false, mensaje = "No se pudo identificar el usuario." });

        try
        {
            var abono = await fiadoRepository.RegistrarAbonoAsync(
                request.IdCliente, idUsuario, request.Monto, request.IdMetodoPago, request.Observaciones);

            // Datos actualizados del cliente para refrescar la vista
            var cliente = await fiadoRepository.ObtenerClientePorIdAsync(request.IdCliente);
            var deuda   = await fiadoRepository.ObtenerBoletasFiadasPorClienteAsync(request.IdCliente);

            return Json(new
            {
                ok = true,
                mensaje = $"Abono de ${abono.Monto:N0} registrado correctamente.",
                saldoAFavor = cliente?.SaldoAFavor ?? 0,
                deudaTotal  = deuda.Sum(b => b.MontoTotal)
            });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, mensaje = ex.Message });
        }
    }

    // ── GET: buscar clientes fiado (usado desde Caja) ─────────────────────────
    [HttpGet]
    public async Task<IActionResult> BuscarClientesAjax(string? q)
    {
        var clientes = await fiadoRepository.ObtenerClientesAsync(q);
        var result = clientes.Select(c => new
        {
            c.IdCliente,
            c.Rut,
            rutFormato = $"{c.Rut:N0}-{RutHelper.CalcularDv(c.Rut)}",
            nombre = $"{c.Nombres} {c.Apellido1} {c.Apellido2}".Trim(),
            c.Telefono,
            c.SaldoAFavor
        });
        return Json(result);
    }

    // ── POST: crear cliente fiado (desde Caja) ────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearClienteAjax([FromBody] CrearClienteFiadoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombres) || string.IsNullOrWhiteSpace(request.Apellido1))
            return Json(new { ok = false, mensaje = "Nombre y apellido son obligatorios." });

        if (request.Rut <= 0)
            return Json(new { ok = false, mensaje = "RUT inválido." });

        var existente = await fiadoRepository.ObtenerClientePorRutAsync(request.Rut);
        if (existente != null)
            return Json(new
            {
                ok = false,
                mensaje = $"Ya existe un cliente con RUT {request.Rut}-{RutHelper.CalcularDv(request.Rut)}.",
                idCliente = existente.IdCliente
            });

        var cliente = new FiaClientes
        {
            Rut        = request.Rut,
            Nombres    = request.Nombres.Trim(),
            Apellido1  = request.Apellido1.Trim(),
            Apellido2  = request.Apellido2?.Trim(),
            Telefono   = request.Telefono?.Trim(),
            Observaciones = request.Observaciones?.Trim()
        };

        var creado = await fiadoRepository.CrearClienteAsync(cliente);
        return Json(new
        {
            ok = true,
            mensaje = $"Cliente {creado.Nombres} {creado.Apellido1} registrado.",
            idCliente = creado.IdCliente,
            nombre    = $"{creado.Nombres} {creado.Apellido1} {creado.Apellido2}".Trim(),
            rutFormato = $"{creado.Rut:N0}-{RutHelper.CalcularDv(creado.Rut)}"
        });
    }
}

// ── ViewModels ────────────────────────────────────────────────────────────────
public class FiadoIndexViewModel
{
    public List<FiaClientes> Clientes         { get; set; } = [];
    public int TotalGlobalAdeudado            { get; set; }
    public int CantClientesConDeuda           { get; set; }
}

public class FiadoClienteViewModel
{
    public FiaClientes    Cliente       { get; set; } = null!;
    public List<VenBoletas> BoletasFiadas { get; set; } = [];
    public List<FiaAbonos>  Abonos        { get; set; } = [];
    public int DeudaTotal                 { get; set; }
    public List<VenMetodosPago> MetodosPago { get; set; } = [];
}

// ── Records de request ────────────────────────────────────────────────────────
public record RegistrarAbonoRequest(int IdCliente, int Monto, int IdMetodoPago, string? Observaciones);
public record CrearClienteFiadoRequest(
    int Rut, string Nombres, string Apellido1,
    string? Apellido2, string? Telefono, string? Observaciones);
