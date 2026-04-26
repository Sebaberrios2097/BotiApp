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
    public async Task<IActionResult> Generar()
    {
        var vm = new GenerarVentaViewModel
        {
            NombreCajero = ClaimHelper.GetNombreCompleto(User),
            Productos = await ventasRepository.ObtenerProductosDisponiblesAsync(),
            Tipos = await ventasRepository.ObtenerTiposAsync(),
            Marcas = await ventasRepository.ObtenerMarcasAsync(),
            Promociones = await ventasRepository.ObtenerPromocionesActivasAsync(),
            Ofertas = await ventasRepository.ObtenerOfertasActivasAsync()
        };
        return View(vm);
    }

    // ── GET /Ventas/Ventas/Consultar ──────────────────────────────────────────
    public async Task<IActionResult> Consultar()
        => View(await ventasRepository.ObtenerBoletasAsync());

    // ── POST: crear boleta ────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearBoletaAjax([FromBody] CrearBoletaRequest request)
    {
        if (request?.Items is not { Count: > 0 })
            return Json(new { ok = false, mensaje = "Debe agregar al menos un producto." });

        var idUsuario = ClaimHelper.GetIdUsuario(User);
        if (idUsuario == 0)
            return Json(new { ok = false, mensaje = "No se pudo identificar el usuario." });

        var detalles = request.Items.Select(i => new VenBoletaDetalle
        {
            IdProducto = i.IdProducto,
            Cantidad = i.Cantidad,
            PrecioUnitario = i.PrecioUnitario,
            Subtotal = i.Cantidad * i.PrecioUnitario
        }).ToList();

        var boleta = new VenBoletas
        {
            IdUsuario = idUsuario,
            IdEstadoBoleta = 1,
            FechaEmision = DateTime.Now,
            MontoTotal = detalles.Sum(d => d.Subtotal)
        };

        var creada = await ventasRepository.CrearBoletaAsync(boleta, detalles);
        var completa = await ventasRepository.ObtenerPorIdAsync(creada.IdBoleta);

        return Json(new
        {
            ok = true,
            mensaje = $"Boleta N° {creada.IdBoleta} generada por ${creada.MontoTotal:N0}.",
            boleta = MapBoleta(completa!)
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static object MapBoleta(VenBoletas b) => new
    {
        b.IdBoleta,
        fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
        estado = b.IdEstadoBoletaNavigation?.NombreEstadoBoleta ?? "—",
        idEstado = b.IdEstadoBoleta,
        cajero = b.IdUsuarioNavigation?.IdEmpleadoNavigation is { } e
                           ? $"{e.NombresEmpleado} {e.Apellido1}"
                           : "—",
        b.MontoTotal,
        detalle = b.VenBoletaDetalle.Select(d => new
        {
            nombre = d.IdProductoNavigation?.NombreProducto ?? "—",
            d.Cantidad,
            d.PrecioUnitario,
            d.Subtotal
        })
    };
}

// ── Records de request ────────────────────────────────────────────────────────
public record CrearBoletaRequest(List<ItemBoleta> Items);
public record ItemBoleta(int IdProducto, int Cantidad, int PrecioUnitario);