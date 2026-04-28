using BotiApp.Areas.Ventas.Models;
using BotiApp.Helpers;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotiApp.Areas.Ventas.Controllers;

[Area("Ventas")]
[Authorize]
public class VentasController(IVentasRepository ventasRepository) : Controller
{
    // ── GET /Ventas/Ventas/Generar ────────────────────────────────────────────
    [Authorize(Policy = "AdminOVendedor")]
    public async Task<IActionResult> Generar()
    {
        var vm = new GenerarVentaViewModel
        {
            NombreVendedor = ClaimHelper.GetNombreCompleto(User),
            Productos      = await ventasRepository.ObtenerProductosDisponiblesAsync(),
            Tipos          = await ventasRepository.ObtenerTiposAsync(),
            Marcas         = await ventasRepository.ObtenerMarcasAsync(),
            Promociones    = await ventasRepository.ObtenerPromocionesActivasAsync(),
            Ofertas        = await ventasRepository.ObtenerOfertasActivasAsync()
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
            Productos   = await ventasRepository.ObtenerProductosDisponiblesAsync(),
            Ofertas     = await ventasRepository.ObtenerOfertasActivasAsync()
        };
        return View(vm);
    }

    // ── GET /Ventas/Ventas/Historial ──────────────────────────────────────────
    public async Task<IActionResult> Historial()
    {
        var esAdmin       = ClaimHelper.EsAdmin(User);
        var esCajero      = ClaimHelper.EsCajero(User);
        var idUsuario     = ClaimHelper.GetIdUsuario(User);
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
            EsAdmin             = esAdmin,
            EsCajero            = esCajero,
            IdUsuarioActual     = idUsuario,
            NombreUsuarioActual = nombreUsuario,
            Boletas             = boletasDtos,
            Vendedores          = vendedores
        };

        return View(vm);
    }

    // ── GET /Ventas/Ventas/Consultar ──────────────────────────────────────────
    public async Task<IActionResult> Consultar()
        => View(await ventasRepository.ObtenerBoletasAsync());

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
            IdProducto       = i.IdProducto,
            Cantidad         = i.Cantidad,
            PrecioNormal     = i.PrecioNormal,
            PrecioUnitario   = i.PrecioUnitario,
            Subtotal         = i.Cantidad * i.PrecioUnitario,
            IdPromocion      = i.IdPromocion,
            IdOfertaProducto = i.IdOfertaProducto
        }).ToList();

        var boleta = new VenBoletas
        {
            IdVendedor     = idVendedor,
            IdEstadoBoleta = 1,
            FechaEmision   = DateTime.Now,
            MontoTotal     = detalles.Sum(d => d.Subtotal)
        };

        var creada   = await ventasRepository.CrearBoletaAsync(boleta, detalles);
        var completa = await ventasRepository.ObtenerPorIdAsync(creada.IdBoleta);

        return Json(new
        {
            ok      = true,
            mensaje = $"Boleta N° {creada.IdBoleta} generada por ${creada.MontoTotal:N0}.",
            boleta  = MapBoletaTicket(completa!)
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
            IdProducto       = i.IdProducto,
            Cantidad         = i.Cantidad,
            PrecioNormal     = i.PrecioNormal,
            PrecioUnitario   = i.PrecioUnitario,
            Subtotal         = i.Cantidad * i.PrecioUnitario,
            IdPromocion      = i.IdPromocion,
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
        var metodos  = request.MetodosPago.Select(m => new VenMetodosPagoBoleta
        {
            IdMetodoPago = m.IdMetodoPago,
            Monto        = m.Monto
        });

        var cobrada = await ventasRepository.CobrarBoletaAsync(request.IdBoleta, idCajero, metodos);
        if (cobrada == null)
            return Json(new { ok = false, mensaje = "La boleta no existe o ya fue procesada." });

        return Json(new
        {
            ok      = true,
            mensaje = $"Boleta N° {cobrada.IdBoleta} cobrada exitosamente por ${cobrada.MontoTotal:N0}.",
            boleta  = MapBoletaCaja(cobrada)
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
            return Json(new { ok = false, mensaje = "La boleta no existe o no está en estado Generada." });

        return Json(new { ok = true, mensaje = $"Boleta N° {request.IdBoleta} anulada correctamente." });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static object MapBoletaTicket(VenBoletas b) => new
    {
        b.IdBoleta,
        fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
        estado       = b.IdEstadoBoletaNavigation?.NombreEstadoBoleta ?? "—",
        idEstado     = b.IdEstadoBoleta,
        vendedor     = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } e
                           ? $"{e.NombresEmpleado} {e.Apellido1}" : "—",
        b.MontoTotal,
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
        estado       = b.IdEstadoBoletaNavigation?.NombreEstadoBoleta ?? "—",
        idEstado     = b.IdEstadoBoleta,
        vendedor     = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } ev
                           ? $"{ev.NombresEmpleado} {ev.Apellido1}" : "—",
        cajero       = b.IdCajeroNavigation?.IdEmpleadoNavigation is { } ec
                           ? $"{ec.NombresEmpleado} {ec.Apellido1}" : (string?)null,
        b.MontoTotal,
        detalle = b.VenBoletaDetalle.Select(d => new
        {
            idProducto       = d.IdProducto,
            nombre           = d.IdProductoNavigation?.NombreProducto ?? "—",
            d.Cantidad,
            d.PrecioNormal,
            d.PrecioUnitario,
            d.Subtotal,
            idPromocion      = d.IdPromocion,
            nombrePromocion  = d.IdPromocionNavigation?.Nombre,
            idOferta         = d.IdOfertaProducto,
            tieneOferta      = d.IdOfertaProducto != null,
            esEnvase         = d.PrecioNormal == 0 && d.IdPromocion == null && d.IdOfertaProducto == null
        })
    };
}

// ── Records de request ────────────────────────────────────────────────────────
public record CrearBoletaRequest(List<ItemBoleta> Items);
public record ItemBoleta(int IdProducto, int Cantidad, int PrecioNormal, int PrecioUnitario, int? IdPromocion, int? IdOfertaProducto);
public record BuscarBoletaRequest(int IdBoleta);
public record ModificarBoletaRequest(int IdBoleta, List<ItemBoleta> Items);
public record CobrarBoletaRequest(int IdBoleta, List<MetodoPagoItem> MetodosPago);
public record MetodoPagoItem(int IdMetodoPago, int Monto);
