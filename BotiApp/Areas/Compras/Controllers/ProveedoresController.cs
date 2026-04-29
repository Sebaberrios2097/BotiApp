using BotiApp.Helpers;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotiApp.Areas.Compras.Controllers;

[Area("Compras")]
[Authorize(Policy = "SoloAdmin")]
public class ProveedoresController(IProveedoresRepository repo) : Controller
{
    // ── Index ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
        => View(await repo.ObtenerTodosAsync());

    // ── Modales ───────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult ModalCrear()
        => PartialView("_ModalCrear", new ProProveedores());

    [HttpGet]
    public async Task<IActionResult> ModalEditar(int id)
    {
        var proveedor = await repo.ObtenerPorIdAsync(id);
        if (proveedor is null) return NotFound();
        return PartialView("_ModalEditar", proveedor);
    }

    [HttpGet]
    public async Task<IActionResult> ModalProductos(int id)
    {
        var proveedor = await repo.ObtenerPorIdAsync(id);
        if (proveedor is null) return NotFound();

        ViewBag.Proveedor          = proveedor;
        ViewBag.ProductosAsociados = await repo.ObtenerProductosAsync(id);
        ViewBag.ProductosLibres    = await repo.ObtenerProductosDisponiblesAsync(id);
        return PartialView("_ModalProductos");
    }

    // ── AJAX POST ─────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAjax(ProProveedores proveedor)
    {
        ModelState.Remove(nameof(proveedor.ComOrdenCompra));
        ModelState.Remove(nameof(proveedor.ComOrdenDetalle));
        ModelState.Remove(nameof(proveedor.ProProveedoresDiasEntrega));
        ModelState.Remove(nameof(proveedor.ProProveedoresProductos));

        if (!ModelState.IsValid)
            return Json(new { ok = false, errores = ObtenerErrores() });

        var creado = await repo.CrearAsync(proveedor);
        return Json(new
        {
            ok      = true,
            mensaje = $"Proveedor «{creado.NombreProveedor}» creado correctamente.",
            fila    = MapFila(creado)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarAjax(int id, ProProveedores proveedor)
    {
        if (id != proveedor.IdProveedor) return BadRequest();

        ModelState.Remove(nameof(proveedor.ComOrdenCompra));
        ModelState.Remove(nameof(proveedor.ComOrdenDetalle));
        ModelState.Remove(nameof(proveedor.ProProveedoresDiasEntrega));
        ModelState.Remove(nameof(proveedor.ProProveedoresProductos));

        if (!ModelState.IsValid)
            return Json(new { ok = false, errores = ObtenerErrores() });

        var actualizado = await repo.ActualizarAsync(proveedor);
        return Json(new
        {
            ok      = true,
            mensaje = $"Proveedor «{actualizado.NombreProveedor}» actualizado.",
            fila    = MapFila(actualizado)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEstado(int id)
    {
        var ok = await repo.ToggleEstadoAsync(id);
        if (!ok) return Json(new { ok = false, mensaje = "Proveedor no encontrado." });

        var proveedor = await repo.ObtenerPorIdAsync(id);
        return Json(new
        {
            ok      = true,
            activo  = proveedor!.Estado,
            mensaje = proveedor.Estado ? "Proveedor activado." : "Proveedor desactivado."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsociarProducto(int idProveedor, int idProducto, int precioProveedor = 0)
    {
        var rel = await repo.AsociarProductoAsync(idProveedor, idProducto, precioProveedor);
        ViewBag.Proveedor          = await repo.ObtenerPorIdAsync(idProveedor);
        ViewBag.ProductosAsociados = await repo.ObtenerProductosAsync(idProveedor);
        ViewBag.ProductosLibres    = await repo.ObtenerProductosDisponiblesAsync(idProveedor);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarPrecioProducto(int idProveedorProducto, int idProveedor, int precioProveedor)
    {
        var ok = await repo.ActualizarPrecioProductoAsync(idProveedorProducto, precioProveedor);
        return Json(new { ok, mensaje = ok ? "Precio actualizado." : "Relación no encontrada." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DesasociarProducto(int id, int idProveedor)
    {
        var ok = await repo.DesasociarProductoAsync(id);
        return Json(new { ok });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private List<string> ObtenerErrores()
        => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

    private static object MapFila(ProProveedores p) => new
    {
        idProveedor         = p.IdProveedor,
        nombreProveedor     = p.NombreProveedor,
        descripcionProveedor = p.DescripcionProveedor,
        estado              = p.Estado
    };
}
