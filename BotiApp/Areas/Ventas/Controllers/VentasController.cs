using BotiApp.Areas.Ventas.Models;
using BotiApp.Helpers;
using BotiApp.Hubs;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BotiApp.Areas.Ventas.Controllers;

[Area("Ventas")]
[Authorize]
public class VentasController(IVentasRepository ventasRepository, IHubContext<BoletaHub> boletaHub) : Controller
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
            Promociones = await ventasRepository.ObtenerPromocionesActivasAsync()
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
            b.CorrelativoDiario,
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
    [Authorize(Policy = "TodosRoles")]
    public async Task<IActionResult> CrearBoletaAjax([FromBody] CrearBoletaRequest request)
    {
        var itemsList = request?.Items ?? new List<ItemBoleta>();

        var idVendedor = ClaimHelper.GetIdUsuario(User);
        if (idVendedor == 0)
            return Json(new { ok = false, mensaje = "No se pudo identificar el usuario." });

        var detalles = itemsList.Select(i => {
            var isOtros = i.IdProducto == 0;
            var cantidad = isOtros ? 1 : i.Cantidad;
            var precioNormal = isOtros ? i.PrecioUnitario : i.PrecioNormal;
            var subtotal = isOtros ? i.PrecioUnitario : (i.Subtotal ?? i.Cantidad * i.PrecioUnitario);
            return new VenBoletaDetalle
            {
                IdProducto = i.IdProducto,
                Cantidad = cantidad,
                PrecioNormal = precioNormal,
                PrecioUnitario = i.PrecioUnitario,
                Subtotal = subtotal,
                IdPromocion = isOtros ? null : i.IdPromocion,
                IdOfertaProducto = isOtros ? null : i.IdOfertaProducto
            };
        }).ToList();

        // Si la venta venía de una postergada se emite sobre esa misma boleta, para
        // que conserve el número que el vendedor ya tenía a la vista en el panel
        VenBoletas? creada = null;
        if (request?.IdBoletaPostergada is { } idPostergada)
            creada = await ventasRepository.EmitirVentaPostergadaAsync(idPostergada, idVendedor, detalles);

        // Sin postergada de origen, o si ya no existe (otro cajero la descartó)
        creada ??= await ventasRepository.CrearBoletaAsync(new VenBoletas
        {
            IdVendedor = idVendedor,
            IdEstadoBoleta = 1,
            FechaEmision = DateTime.Now,
            MontoTotal = detalles.Sum(d => d.Subtotal)
        }, detalles);

        var completa = await ventasRepository.ObtenerPorIdAsync(creada.IdBoleta);

        // Notificar a todos los cajeros conectados en tiempo real
        await boletaHub.Clients.All.SendAsync("NuevaBoleta");

        return Json(new
        {
            ok = true,
            mensaje = $"Boleta N° {creada.CorrelativoDiario ?? creada.IdBoleta}{(creada.CorrelativoDiario != null ? $" (ID: {creada.IdBoleta})" : "")} generada por ${creada.MontoTotal:N0}.",
            boleta = MapBoletaCaja(completa!)
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
        {
            boleta = await ventasRepository.ObtenerBoletaPorCorrelativoDiarioAsync(request.IdBoleta, DateTime.Now);
        }

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

        var detalles = request.Items.Select(i => {
            var isOtros = i.IdProducto == 0;
            var cantidad = isOtros ? 1 : i.Cantidad;
            var precioNormal = isOtros ? i.PrecioUnitario : i.PrecioNormal;
            var subtotal = isOtros ? i.PrecioUnitario : (i.Subtotal ?? i.Cantidad * i.PrecioUnitario);
            return new VenBoletaDetalle
            {
                IdProducto = i.IdProducto,
                Cantidad = cantidad,
                PrecioNormal = precioNormal,
                PrecioUnitario = i.PrecioUnitario,
                Subtotal = subtotal,
                IdPromocion = isOtros ? null : i.IdPromocion,
                IdOfertaProducto = isOtros ? null : i.IdOfertaProducto
            };
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

        return Json(new
        {
            ok = true,
            mensaje = $"Boleta N° {cobrada.CorrelativoDiario ?? cobrada.IdBoleta}{(cobrada.CorrelativoDiario != null ? $" (ID: {cobrada.IdBoleta})" : "")} cobrada exitosamente por ${cobrada.MontoTotal:N0}.",
            boleta = MapBoletaCaja(cobrada)
        });
    }

    // ── POST: anular boleta ───────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOCajero")]
    public async Task<IActionResult> AnularBoletaAjax([FromBody] AnularBoletaRequest request)
    {
        var idUsuario = ClaimHelper.GetIdUsuario(User);

        // Traza de la anulación en Observaciones: quién, cuándo y por qué. Es la única
        // forma de saber quién anuló una boleta ya cobrada, porque Id_Cajero conserva
        // al cajero original.
        var motivo = request.Motivo?.Trim();
        if (motivo is { Length: > 200 }) motivo = motivo[..200];

        var nota = $"[Anulada el {DateTime.Now:dd-MM-yyyy HH:mm} por {ClaimHelper.GetNombreCompleto(User)}]"
                 + (string.IsNullOrWhiteSpace(motivo) ? "" : $" Motivo: {motivo}");

        var ok = await ventasRepository.AnularBoletaAsync(request.IdBoleta, idUsuario, nota);
        if (!ok)
            return Json(new { ok = false, mensaje = "La boleta no existe o ya estaba anulada." });

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
            mensaje = $"Boleta N° {boleta.CorrelativoDiario ?? boleta.IdBoleta}{(boleta.CorrelativoDiario != null ? $" (ID: {boleta.IdBoleta})" : "")} registrada como fiado.",
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
            correlativoDiario = b.CorrelativoDiario,
            fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
            vendedor = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } ev
                ? $"{ev.NombresEmpleado} {ev.Apellido1}".Trim()
                : "—",
            montoTotal = b.MontoTotal,
            cantProductos = b.VenBoletaDetalle.Count
        });
        return Json(result);
    }

    // ── Ventas postergadas (estado 5 «Sin Finalizar») ─────────────────────────

    // POST: deja el carrito guardado para atender a otro cliente
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TodosRoles")]
    public async Task<IActionResult> PostergarVentaAjax([FromBody] PostergarVentaRequest request)
    {
        if (request?.Items is not { Count: > 0 })
            return Json(new { ok = false, mensaje = "No hay productos que postergar." });

        var idVendedor = ClaimHelper.GetIdUsuario(User);
        if (idVendedor == 0)
            return Json(new { ok = false, mensaje = "No se pudo identificar el usuario." });

        var detalles = request.Items.Select(i => new VenBoletaDetalle
        {
            IdProducto       = i.IdProducto,
            Cantidad         = i.Cantidad,
            PrecioNormal     = i.PrecioNormal,
            PrecioUnitario   = i.PrecioUnitario,
            Subtotal         = i.Subtotal ?? i.Cantidad * i.PrecioUnitario,
            IdPromocion      = i.IdPromocion,
            IdOfertaProducto = i.IdOfertaProducto
        }).ToList();

        // Total ya con descuentos, que es el monto que el vendedor reconoce al retomarla
        var montoTotal = request.MontoTotal > 0
            ? request.MontoTotal
            : detalles.Sum(d => d.Subtotal);

        // Si venía de una venta ya postergada se actualiza en su lugar, para que
        // reeditarla no la renumere ni deje copias sueltas en el panel
        if (request.IdBoleta is { } idExistente)
        {
            var actualizada = await ventasRepository.ActualizarVentaPostergadaAsync(
                idExistente, montoTotal, detalles);

            if (actualizada is not null)
                return Json(new
                {
                    ok = true,
                    mensaje = $"Venta postergada N° {NumeroVenta(actualizada)} actualizada.",
                    venta = MapVentaPostergada(actualizada, detalles.Count)
                });

            // Ya no existe (otro cajero la descartó): se guarda como una nueva
        }

        var boleta = new VenBoletas
        {
            IdVendedor   = idVendedor,
            FechaEmision = DateTime.Now,
            MontoTotal   = montoTotal
        };

        var creada = await ventasRepository.PostergarVentaAsync(boleta, detalles);

        return Json(new
        {
            ok = true,
            mensaje = $"Venta postergada como N° {NumeroVenta(creada)}.",
            venta = MapVentaPostergada(creada, detalles.Count)
        });
    }

    [HttpGet]
    [Authorize(Policy = "TodosRoles")]
    public async Task<IActionResult> VentasPostergadasAjax()
    {
        var ventas = await ventasRepository.ObtenerVentasPostergadasAsync(top: 30);
        return Json(ventas.Select(b => MapVentaPostergada(b, b.VenBoletaDetalle.Count)));
    }

    // POST: devuelve los productos al carrito; la venta postergada se conserva
    // hasta que se emita o se vacíe el carrito (DescartarVentaPostergadaAjax)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TodosRoles")]
    public async Task<IActionResult> RecuperarVentaPostergadaAjax([FromBody] BuscarBoletaRequest request)
    {
        var boleta = await ventasRepository.RecuperarVentaPostergadaAsync(request.IdBoleta);
        if (boleta is null)
            return Json(new { ok = false, mensaje = "La venta postergada ya no existe." });

        return Json(new
        {
            ok = true,
            mensaje = $"Venta N° {NumeroVenta(boleta)} cargada en el carrito.",
            items = boleta.VenBoletaDetalle.Select(d => new
            {
                idProducto     = d.IdProducto,
                cantidad       = d.Cantidad,
                precioNormal   = d.PrecioNormal,
                precioUnitario = d.PrecioUnitario
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TodosRoles")]
    public async Task<IActionResult> DescartarVentaPostergadaAjax([FromBody] BuscarBoletaRequest request)
    {
        var numero = await ventasRepository.DescartarVentaPostergadaAsync(request.IdBoleta);
        return Json(new
        {
            ok = numero is not null,
            mensaje = numero is { } n
                ? $"Venta postergada N° {n} descartada."
                : "La venta postergada ya no existe."
        });
    }

    // Número que ve el vendedor: el correlativo diario cuando existe, con el id
    // interno solo como respaldo (misma convención que el resto de las vistas).
    private static int NumeroVenta(VenBoletas b) => b.CorrelativoDiario ?? b.IdBoleta;

    private static object MapVentaPostergada(VenBoletas b, int cantItems) => new
    {
        idBoleta      = b.IdBoleta,
        numero        = NumeroVenta(b),
        montoTotal    = b.MontoTotal,
        cantProductos = cantItems,
        hora          = b.FechaEmision?.ToString("HH:mm") ?? "—",
        fechaEmision  = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
        vendedor      = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } ev
            ? $"{ev.NombresEmpleado} {ev.Apellido1}".Trim()
            : "—"
    };

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static object MapBoletaTicket(VenBoletas b) => new
    {
        b.IdBoleta,
        correlativoDiario = b.CorrelativoDiario,
        fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
        estado = b.IdEstadoBoletaNavigation?.NombreEstadoBoleta ?? "—",
        idEstado = b.IdEstadoBoleta,
        vendedor = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } e
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
        correlativoDiario = b.CorrelativoDiario,
        fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
        estado = b.IdEstadoBoletaNavigation?.NombreEstadoBoleta ?? "—",
        idEstado = b.IdEstadoBoleta,
        vendedor = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } ev
                           ? $"{ev.NombresEmpleado} {ev.Apellido1}" : "—",
        cajero = b.IdCajeroNavigation?.IdEmpleadoNavigation is { } ec
                           ? $"{ec.NombresEmpleado} {ec.Apellido1}" : (string?)null,
        b.MontoTotal,
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
/// <param name="IdBoletaPostergada">Venta postergada que se está emitiendo, para reutilizar su boleta y su correlativo; null para una venta nueva.</param>
public record CrearBoletaRequest(List<ItemBoleta> Items, int? IdBoletaPostergada = null);
public record ItemBoleta(int IdProducto, int Cantidad, int PrecioNormal, int PrecioUnitario, int? IdPromocion, int? IdOfertaProducto, int? Subtotal = null);
public record BuscarBoletaRequest(int IdBoleta);
public record AnularBoletaRequest(int IdBoleta, string? Motivo = null);
public record ModificarBoletaRequest(int IdBoleta, List<ItemBoleta> Items);
public record CobrarBoletaRequest(int IdBoleta, List<MetodoPagoItem> MetodosPago);
public record DejarFiadoRequest(int IdBoleta, int IdClienteFiado);
/// <param name="IdBoleta">Venta postergada que se está reeditando; null para una nueva.</param>
public record PostergarVentaRequest(List<ItemBoleta> Items, int MontoTotal = 0, int? IdBoleta = null);
public record MetodoPagoItem(int IdMetodoPago, int Monto);
