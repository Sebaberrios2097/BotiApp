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
    /// <summary>Largo de Fia_Abonos.Observaciones en la base de datos.</summary>
    public const int MaxObservaciones = 300;


    // ── GET /Ventas/Fiado ─────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        // Se traen también los desactivados: este es el único lugar donde se
        // administran, y sin ellos no habría cómo reactivarlos ni eliminarlos.
        var clientes = await fiadoRepository.ObtenerClientesAsync(soloActivos: false);

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

        var boletasActivas  = await fiadoRepository.ObtenerBoletasFiadasPorClienteAsync(id);
        var historial       = await fiadoRepository.ObtenerBoletasHistorialAsync(id);
        var abonos          = await fiadoRepository.ObtenerAbonosPorClienteAsync(id);
        var metodosPago     = await context.VenMetodosPago.AsNoTracking().ToListAsync();

        var vm = new FiadoClienteViewModel
        {
            Cliente          = cliente,
            BoletasHistorial = historial.ToList(),
            Abonos           = abonos.ToList(),
            DeudaTotal       = boletasActivas.Sum(b => b.MontoTotal),
            MetodosPago      = metodosPago
        };
        return View(vm);
    }

    // ── POST: registrar abono ─────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAbonoAjax([FromBody] RegistrarAbonoRequest request)
    {
        if (request.Metodos == null || request.Metodos.Count == 0)
            return Json(new { ok = false, mensaje = "Agrega al menos un método de pago." });

        if (request.Metodos.Any(m => m.IdMetodoPago <= 0))
            return Json(new { ok = false, mensaje = "Todos los métodos de pago deben ser válidos." });

        if (request.Metodos.Any(m => m.Monto <= 0))
            return Json(new { ok = false, mensaje = "El monto de cada método debe ser mayor a 0." });

        // La columna admite 300 caracteres: se corta antes de llegar a la base de
        // datos para no fallar con un error de truncamiento
        if (request.Observaciones is { Length: > MaxObservaciones })
            return Json(new { ok = false, mensaje = $"Las observaciones no pueden superar {MaxObservaciones} caracteres." });

        var idUsuario = ClaimHelper.GetIdUsuario(User);
        if (idUsuario == 0)
            return Json(new { ok = false, mensaje = "No se pudo identificar el usuario." });

        try
        {
            // Deuda antes del abono, para saber qué boletas saldó el pago FIFO
            var pendientesAntes = (await fiadoRepository
                .ObtenerBoletasFiadasPorClienteAsync(request.IdCliente)).ToList();

            var items = request.Metodos.Select(m => new AbonoMetodoItem(m.IdMetodoPago, m.Monto));
            var abonos = (await fiadoRepository.RegistrarAbonoAsync(
                request.IdCliente, idUsuario, items, request.Observaciones)).ToList();

            // Cargar nombres de métodos para la respuesta
            var metodoIds = abonos.Select(a => a.IdMetodoPago).Distinct().ToList();
            var metodos = await context.VenMetodosPago
                .Where(m => metodoIds.Contains(m.IdMetodoPago))
                .AsNoTracking()
                .ToListAsync();

            var totalMonto = abonos.Sum(a => a.Monto);

            // Datos actualizados del cliente para refrescar la vista
            var cliente   = await fiadoRepository.ObtenerClientePorIdAsync(request.IdCliente);
            var pendientes = (await fiadoRepository.ObtenerBoletasFiadasPorClienteAsync(request.IdCliente)).ToList();

            var usuario = await context.EmpUsuario
                .AsNoTracking()
                .Include(u => u.IdEmpleadoNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
            var nombreUsuario = usuario?.IdEmpleadoNavigation is { } emp
                ? $"{emp.NombresEmpleado} {emp.Apellido1}".Trim()
                : "—";

            // Boletas que el abono acaba de saldar, para poder avisarlo en pantalla
            var idsPendientesAhora = pendientes.Select(b => b.IdBoleta).ToHashSet();
            var saldadas = pendientesAntes
                .Where(b => !idsPendientesAhora.Contains(b.IdBoleta))
                .Select(b => new
                {
                    idBoleta = b.IdBoleta,
                    numero   = b.CorrelativoDiario ?? b.IdBoleta,
                    monto    = b.MontoTotal
                })
                .ToArray();

            return Json(new
            {
                ok = true,
                mensaje     = $"Abono de ${totalMonto:N0} registrado correctamente.",
                saldoAFavor = cliente?.SaldoAFavor ?? 0,
                deudaTotal  = pendientes.Sum(b => b.MontoTotal),
                cantPendientes = pendientes.Count,
                saldadas,
                abonos = abonos.Select(a => new
                {
                    fecha         = a.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    metodoPago    = metodos.FirstOrDefault(m => m.IdMetodoPago == a.IdMetodoPago)?.NombreMetodoPago ?? "—",
                    monto         = a.Monto,
                    observaciones = a.Observaciones,
                    usuario       = nombreUsuario
                }).ToArray()
            });
        }
        catch (DbUpdateException ex)
        {
            // El mensaje de EF ("See the inner exception for details") no dice nada:
            // se expone la causa real de la base de datos para poder diagnosticarla.
            var causa = ex.GetBaseException().Message;
            return Json(new { ok = false, mensaje = $"No se pudo guardar el abono: {causa}" });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, mensaje = ex.Message });
        }
    }

    // ── POST: desactivar / reactivar / eliminar cliente ───────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DesactivarClienteAjax([FromBody] ClienteFiadoRequest request)
    {
        var r = await fiadoRepository.DesactivarClienteAsync(request.IdCliente);
        return Json(new { ok = r.Ok, mensaje = r.Mensaje, estado = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivarClienteAjax([FromBody] ClienteFiadoRequest request)
    {
        var r = await fiadoRepository.ReactivarClienteAsync(request.IdCliente);
        return Json(new { ok = r.Ok, mensaje = r.Mensaje, estado = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarClienteAjax([FromBody] ClienteFiadoRequest request)
    {
        try
        {
            var r = await fiadoRepository.EliminarClienteAsync(request.IdCliente);
            return Json(new
            {
                ok = r.Ok,
                mensaje = r.Mensaje,
                redirigirA = r.Ok ? Url.Action("Index", "Fiado", new { area = "Ventas" }) : null
            });
        }
        catch (DbUpdateException ex)
        {
            return Json(new { ok = false, mensaje = $"No se pudo eliminar el cliente: {ex.GetBaseException().Message}" });
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
            nombre    = c.Nombre,
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
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return Json(new { ok = false, mensaje = "El nombre es obligatorio." });

        var cliente = new FiaClientes
        {
            Nombre   = request.Nombre.Trim(),
            Telefono = request.Telefono?.Trim()
        };

        var creado = await fiadoRepository.CrearClienteAsync(cliente);
        return Json(new
        {
            ok = true,
            mensaje   = $"Cliente {creado.Nombre} registrado.",
            idCliente = creado.IdCliente,
            nombre    = creado.Nombre
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
    public FiaClientes          Cliente          { get; set; } = null!;
    public List<VenBoletas>     BoletasHistorial { get; set; } = [];
    public List<FiaAbonos>      Abonos           { get; set; } = [];
    public int                  DeudaTotal       { get; set; }
    public List<VenMetodosPago> MetodosPago      { get; set; } = [];
}

// ── Records de request ────────────────────────────────────────────────────────
public record AbonoMetodoDto(int IdMetodoPago, int Monto);
public record RegistrarAbonoRequest(int IdCliente, List<AbonoMetodoDto> Metodos, string? Observaciones);
public record CrearClienteFiadoRequest(string Nombre, string? Telefono);
public record ClienteFiadoRequest(int IdCliente);
