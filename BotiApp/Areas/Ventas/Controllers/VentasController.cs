using BotiApp.Areas.Ventas.Models;
using BotiApp.Helpers;
using BotiApp.Hubs;
using BotiApp.Services.Sii;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BotiApp.Areas.Ventas.Controllers;

[Area("Ventas")]
[Authorize]
public class VentasController(
    IVentasRepository ventasRepository,
    IHubContext<BoletaHub> boletaHub,
    ISiiBoletaService siiBoletaService,
    IOptions<SiiEmisorOptions> siiEmisorOptions) : Controller
{
    // ── GET /Ventas/Ventas/Generar ────────────────────────────────────────────
    [Authorize(Policy = "AdminOVendedor")]
    public async Task<IActionResult> Generar()
    {
        var vm = new GenerarVentaViewModel
        {
            NombreVendedor = ClaimHelper.GetNombreCompleto(User),
            Productos = await ventasRepository.ObtenerProductosDisponiblesAsync(),
            Tipos = await ventasRepository.ObtenerTiposAsync(),
            Marcas = await ventasRepository.ObtenerMarcasAsync(),
            Promociones = await ventasRepository.ObtenerPromocionesActivasAsync(),
            Ofertas = await ventasRepository.ObtenerOfertasActivasAsync()
        };
        return View(vm);
    }

    // ── GET /Ventas/Ventas/Caja ───────────────────────────────────────────────
    public async Task<IActionResult> Caja()
    {
        var vm = new CajaViewModel
        {
            PuedeCobrar = ClaimHelper.EsCajero(User) || ClaimHelper.EsAdmin(User),
            MetodosPago = await ventasRepository.ObtenerMetodosPagoAsync(),
            Productos = await ventasRepository.ObtenerProductosDisponiblesAsync(),
            Ofertas = await ventasRepository.ObtenerOfertasActivasAsync(),
            Promociones = await ventasRepository.ObtenerPromocionesActivasAsync(),
            EmisorSii = MapEmisorSii(siiEmisorOptions.Value)
        };
        return View(vm);
    }

    // ── GET /Ventas/Ventas/Historial ──────────────────────────────────────────
    public async Task<IActionResult> Historial()
    {
        var esAdmin = ClaimHelper.EsAdmin(User);
        var esCajero = ClaimHelper.EsCajero(User);
        var idUsuario = ClaimHelper.GetIdUsuario(User);
        var nombreUsuario = ClaimHelper.GetNombreCompleto(User);

        var boletas = esAdmin
            ? await ventasRepository.ObtenerBoletasAsync()
            : esCajero
                ? await ventasRepository.ObtenerBoletasPorCajeroAsync(idUsuario, top: 100)
                : await ventasRepository.ObtenerBoletasPorVendedorAsync(idUsuario, top: 100);

        var boletasDtos = boletas.Select(b => new BoletaResumenDto(
            b.IdBoleta,
            b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
            b.IdEstadoBoletaNavigation?.NombreEstadoBoleta ?? "—",
            b.IdEstadoBoleta,
            b.IdVendedor,
            b.IdVendedorNavigation?.IdEmpleadoNavigation is { } ev
                ? $"{ev.NombresEmpleado} {ev.Apellido1}"
                : "—",
            b.IdCajero,
            b.IdCajeroNavigation?.IdEmpleadoNavigation is { } ec
                ? $"{ec.NombresEmpleado} {ec.Apellido1}"
                : null,
            b.MontoTotal,
            b.TipoDteSii,
            b.FolioSii,
            b.EstadoSii,
            b.TrackIdSii,
            b.FechaEnvioSii?.ToString("dd/MM/yyyy HH:mm") ?? "—",
            b.MensajeSii,
            b.VenBoletaDetalle.Select(d => new DetalleBoletaDto(
                d.IdProductoNavigation?.NombreProducto ?? "—",
                d.Cantidad,
                d.PrecioUnitario,
                d.PrecioNormal,
                d.Subtotal,
                d.IdPromocionNavigation?.Nombre,
                d.IdOfertaProductoNavigation != null ? "Oferta" : null
            ))
        )).ToList();

        var vendedores = esAdmin
            ? (IEnumerable<VendedorFiltroDto>)boletasDtos
                .GroupBy(b => b.IdVendedor)
                .Select(g => new VendedorFiltroDto(g.Key, g.First().Vendedor))
                .OrderBy(v => v.Nombre)
                .ToList()
            : [];

        var vm = new HistorialVentasViewModel
        {
            EsAdmin = esAdmin,
            EsCajero = esCajero,
            IdUsuarioActual = idUsuario,
            NombreUsuarioActual = nombreUsuario,
            Boletas = boletasDtos,
            Vendedores = vendedores
        };

        return View(vm);
    }

    // ── POST: crear boleta (vendedor) ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOVendedor")]
    public async Task<IActionResult> CrearBoletaAjax([FromBody] CrearBoletaRequest request)
    {
        if (request?.Items is not { Count: > 0 })
            return Json(new { ok = false, mensaje = "Debe agregar al menos un producto." });

        var idVendedor = ClaimHelper.GetIdUsuario(User);
        if (idVendedor == 0)
            return Json(new { ok = false, mensaje = "No se pudo identificar el usuario." });

        var detalles = request.Items.Select(i => new VenBoletaDetalle
        {
            IdProducto = i.IdProducto,
            Cantidad = i.Cantidad,
            PrecioNormal = i.PrecioNormal,
            PrecioUnitario = i.PrecioUnitario,
            Subtotal = i.Subtotal ?? i.Cantidad * i.PrecioUnitario,
            IdPromocion = i.IdPromocion,
            IdOfertaProducto = i.IdOfertaProducto
        }).ToList();

        var boleta = new VenBoletas
        {
            IdVendedor = idVendedor,
            IdEstadoBoleta = 1,
            FechaEmision = DateTime.Now,
            MontoTotal = detalles.Sum(d => d.Subtotal)
        };

        var creada = await ventasRepository.CrearBoletaAsync(boleta, detalles);
        var completa = await ventasRepository.ObtenerPorIdAsync(creada.IdBoleta);

        // Notificar a todos los cajeros conectados en tiempo real
        await boletaHub.Clients.All.SendAsync("NuevaBoleta");

        return Json(new
        {
            ok = true,
            mensaje = $"Boleta N° {creada.IdBoleta} generada por ${creada.MontoTotal:N0}.",
            boleta = MapBoletaTicket(completa!)
        });
    }

    // ── POST: buscar boleta para Caja ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOCajero")]
    public async Task<IActionResult> BuscarBoletaAjax([FromBody] BuscarBoletaRequest request)
    {
        var boleta = await ventasRepository.ObtenerBoletaParaCajaAsync(request.IdBoleta);
        if (boleta == null)
            return Json(new { ok = false, mensaje = $"Boleta N° {request.IdBoleta} no encontrada." });

        return Json(new { ok = true, boleta = MapBoletaCaja(boleta) });
    }

    // ── POST: modificar detalle de boleta ─────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOCajero")]
    public async Task<IActionResult> ModificarBoletaAjax([FromBody] ModificarBoletaRequest request)
    {
        if (request?.Items is not { Count: > 0 })
            return Json(new { ok = false, mensaje = "Debe haber al menos un producto." });

        var detalles = request.Items.Select(i => new VenBoletaDetalle
        {
            IdProducto = i.IdProducto,
            Cantidad = i.Cantidad,
            PrecioNormal = i.PrecioNormal,
            PrecioUnitario = i.PrecioUnitario,
            Subtotal = i.Subtotal ?? i.Cantidad * i.PrecioUnitario,
            IdPromocion = i.IdPromocion,
            IdOfertaProducto = i.IdOfertaProducto
        });

        var actualizada = await ventasRepository.ModificarBoletaDetalleAsync(request.IdBoleta, detalles);
        if (actualizada == null)
            return Json(new { ok = false, mensaje = "La boleta no existe o no está en estado Generada." });

        return Json(new { ok = true, mensaje = "Boleta actualizada.", boleta = MapBoletaCaja(actualizada) });
    }

    // ── POST: cobrar boleta (solo Cajero/Admin) ───────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOCajero")]
    public async Task<IActionResult> CobrarBoletaAjax([FromBody] CobrarBoletaRequest request)
    {
        if (!ClaimHelper.EsCajero(User) && !ClaimHelper.EsAdmin(User))
            return Json(new { ok = false, mensaje = "Sin permisos para realizar cobros." });

        if (request?.MetodosPago is not { Count: > 0 })
            return Json(new { ok = false, mensaje = "Debe especificar al menos un método de pago." });

        var idCajero = ClaimHelper.GetIdUsuario(User);
        var metodos = request.MetodosPago.Select(m => new VenMetodosPagoBoleta
        {
            IdMetodoPago = m.IdMetodoPago,
            Monto = m.Monto
        });

        var cobrada = await ventasRepository.CobrarBoletaAsync(request.IdBoleta, idCajero, metodos);
        if (cobrada == null)
            return Json(new { ok = false, mensaje = "La boleta no existe o ya fue procesada." });

        SiiBoletaServiceResult sii;
        try
        {
            sii = await siiBoletaService.EmitirBoletaAfectaAsync(cobrada.IdBoleta);
        }
        catch
        {
            sii = new SiiBoletaServiceResult(
                Ok: false,
                EstadoSii: "ERROR_ENVIO",
                Mensaje: "SII simulado: no fue posible emitir en este intento.",
                Folio: null,
                TrackId: null,
                Intentos: 0
            );
        }

        var completa = await ventasRepository.ObtenerBoletaParaCajaAsync(cobrada.IdBoleta);
        var boletaResult = completa ?? cobrada;

        var mensaje = $"Boleta N° {boletaResult.IdBoleta} cobrada exitosamente por ${boletaResult.MontoTotal:N0}.";
        if (!string.IsNullOrWhiteSpace(sii.Mensaje))
            mensaje += $" {sii.Mensaje}";

        return Json(new
        {
            ok = true,
            mensaje,
            boleta = MapBoletaCaja(boletaResult)
        });
    }

    // ── POST: reintentar emisión SII simulada ────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOCajero")]
    public async Task<IActionResult> ReintentarSiiAjax([FromBody] BuscarBoletaRequest request)
    {
        SiiBoletaServiceResult sii;
        try
        {
            sii = await siiBoletaService.EmitirBoletaAfectaAsync(request.IdBoleta, forzarReintento: true);
        }
        catch
        {
            sii = new SiiBoletaServiceResult(
                Ok: false,
                EstadoSii: "ERROR_ENVIO",
                Mensaje: "SII simulado: no fue posible reintentar en este momento.",
                Folio: null,
                TrackId: null,
                Intentos: 0
            );
        }

        var boleta = await ventasRepository.ObtenerBoletaParaCajaAsync(request.IdBoleta);

        if (boleta == null)
            return Json(new { ok = false, mensaje = $"Boleta N° {request.IdBoleta} no encontrada." });

        return Json(new
        {
            ok = true,
            emitido = sii.Ok,
            mensaje = sii.Mensaje,
            boleta = MapBoletaCaja(boleta)
        });
    }

    // ── POST: anular boleta ───────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOCajero")]
    public async Task<IActionResult> AnularBoletaAjax([FromBody] BuscarBoletaRequest request)
    {
        var idUsuario = ClaimHelper.GetIdUsuario(User);
        var ok = await ventasRepository.AnularBoletaAsync(request.IdBoleta, idUsuario);
        if (!ok)
            return Json(new { ok = false, mensaje = "La boleta no existe o no está en estado Generada/Fiado." });

        return Json(new { ok = true, mensaje = $"Boleta N° {request.IdBoleta} anulada correctamente." });
    }

    // ── POST: dejar boleta como fiado ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOCajero")]
    public async Task<IActionResult> DejarFiadoAjax([FromBody] DejarFiadoRequest request)
    {
        if (request.IdClienteFiado <= 0)
            return Json(new { ok = false, mensaje = "Debe seleccionar un cliente fiado." });

        var idCajero = ClaimHelper.GetIdUsuario(User);
        var boleta = await ventasRepository.DejarFiadoAsync(request.IdBoleta, request.IdClienteFiado, idCajero);
        if (boleta == null)
            return Json(new { ok = false, mensaje = "La boleta no existe o ya fue procesada." });

        return Json(new
        {
            ok = true,
            mensaje = $"Boleta N° {boleta.IdBoleta} registrada como fiado.",
            boleta = MapBoletaCaja(boleta)
        });
    }

    // ── GET: boletas pendientes para panel rápido en Caja ────────────────────
    [HttpGet]
    public async Task<IActionResult> BoletasPendientesAjax()
    {
        var boletas = await ventasRepository.ObtenerBoletasPendientesAsync(top: 20);
        var result = boletas.Select(b => new
        {
            idBoleta = b.IdBoleta,
            fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
            vendedor = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } ev
                ? $"{ev.NombresEmpleado} {ev.Apellido1}".Trim()
                : "—",
            montoTotal = b.MontoTotal,
            cantProductos = b.VenBoletaDetalle.Count
        });
        return Json(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static SiiEmisorDto MapEmisorSii(SiiEmisorOptions? options)
        => new()
        {
            Rut = NormalizarCampo(options?.Rut),
            RazonSocial = NormalizarCampo(options?.RazonSocial),
            Giro = NormalizarCampo(options?.Giro),
            Direccion = NormalizarCampo(options?.Direccion),
            Comuna = NormalizarCampo(options?.Comuna),
            Ambiente = NormalizarCampo(options?.Ambiente, "Simulado")
        };

    private static string NormalizarCampo(string? value, string fallback = "NO CONFIGURADO")
        => string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();

    private static object MapBoletaTicket(VenBoletas b) => new
    {
        b.IdBoleta,
        fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
        estado = b.IdEstadoBoletaNavigation?.NombreEstadoBoleta ?? "—",
        idEstado = b.IdEstadoBoleta,
        vendedor = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } e
                           ? $"{e.NombresEmpleado} {e.Apellido1}" : "—",
        b.MontoTotal,
        sii = new
        {
            tipoDte = b.TipoDteSii,
            folio = b.FolioSii,
            estado = b.EstadoSii,
            trackId = b.TrackIdSii,
            fechaEnvio = b.FechaEnvioSii?.ToString("dd/MM/yyyy HH:mm") ?? "—",
            montoNeto = b.MontoNetoSii,
            montoIva = b.MontoIvaSii,
            montoExento = b.MontoExentoSii,
            mensaje = b.MensajeSii,
            intentos = b.IntentosEnvioSii ?? 0
        },
        detalle = b.VenBoletaDetalle.Select(d => new
        {
            nombre = d.IdProductoNavigation?.NombreProducto ?? "—",
            d.Cantidad,
            d.PrecioUnitario,
            d.Subtotal
        })
    };

    private static object MapBoletaCaja(VenBoletas b) => new
    {
        b.IdBoleta,
        fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
        estado = b.IdEstadoBoletaNavigation?.NombreEstadoBoleta ?? "—",
        idEstado = b.IdEstadoBoleta,
        vendedor = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } ev
                           ? $"{ev.NombresEmpleado} {ev.Apellido1}" : "—",
        cajero = b.IdCajeroNavigation?.IdEmpleadoNavigation is { } ec
                           ? $"{ec.NombresEmpleado} {ec.Apellido1}" : (string?)null,
        b.MontoTotal,
        sii = new
        {
            tipoDte = b.TipoDteSii,
            folio = b.FolioSii,
            estado = b.EstadoSii,
            trackId = b.TrackIdSii,
            fechaEnvio = b.FechaEnvioSii?.ToString("dd/MM/yyyy HH:mm") ?? "—",
            montoNeto = b.MontoNetoSii,
            montoIva = b.MontoIvaSii,
            montoExento = b.MontoExentoSii,
            mensaje = b.MensajeSii,
            intentos = b.IntentosEnvioSii ?? 0
        },
        detalle = b.VenBoletaDetalle.Select(d => new
        {
            idProducto = d.IdProducto,
            nombre = d.IdProductoNavigation?.NombreProducto ?? "—",
            d.Cantidad,
            d.PrecioNormal,
            d.PrecioUnitario,
            d.Subtotal,
            idPromocion = d.IdPromocion,
            nombrePromocion = d.IdPromocionNavigation?.Nombre,
            idOferta = d.IdOfertaProducto,
            tieneOferta = d.IdOfertaProducto != null,
            esEnvase = d.PrecioNormal == 0 && d.IdPromocion == null && d.IdOfertaProducto == null
        })
    };
}

// ── Records de request ────────────────────────────────────────────────────────
public record CrearBoletaRequest(List<ItemBoleta> Items);
public record ItemBoleta(int IdProducto, int Cantidad, int PrecioNormal, int PrecioUnitario, int? IdPromocion, int? IdOfertaProducto, int? Subtotal = null);
public record BuscarBoletaRequest(int IdBoleta);
public record ModificarBoletaRequest(int IdBoleta, List<ItemBoleta> Items);
public record CobrarBoletaRequest(int IdBoleta, List<MetodoPagoItem> MetodosPago);
public record DejarFiadoRequest(int IdBoleta, int IdClienteFiado);
public record MetodoPagoItem(int IdMetodoPago, int Monto);
