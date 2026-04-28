using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BotiApp.Areas.Productos.Controllers;

[Area("Productos")]
[Authorize(Policy = "SoloAdmin")]
public class ProductosController(IProductosRepository productosRepository) : Controller
{
    // ── Index ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
        => View(await productosRepository.ObtenerTodosAsync());

    // ── Imagen ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Imagen(int id)
    {
        var p = await productosRepository.ObtenerPorIdAsync(id);
        if (p?.Imagen is null) return NotFound();
        return File(p.Imagen, "image/jpeg");
    }

    // ── Partials para modals ─────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ModalCrear()
    {
        await CargarSelectListsAsync();
        ViewBag.UltimosProductos = await productosRepository.ObtenerUltimosIngresadosAsync(5);
        return PartialView("_ModalCrear", new ProProductos { Estado = true, FechaIngreso = DateTime.Now });
    }

    [HttpGet]
    public async Task<IActionResult> ModalEditar(int id)
    {
        var producto = await productosRepository.ObtenerPorIdAsync(id);
        if (producto is null) return NotFound();

        await CargarSelectListsAsync(producto.IdMarca, producto.IdTipoProducto);
        ViewBag.Auditoria = await productosRepository.ObtenerAuditoriaAsync(id);
        return PartialView("_ModalEditar", producto);
    }

    [HttpGet]
    public async Task<IActionResult> ModalDetalle(int id)
    {
        var producto = await productosRepository.ObtenerPorIdAsync(id);
        if (producto is null) return NotFound();

        ViewBag.Auditoria = await productosRepository.ObtenerAuditoriaAsync(id);
        return PartialView("_ModalDetalle", producto);
    }

    // ── AJAX POST ────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAjax(ProProductos producto, IFormFile? imagenFile)
    {
        ModelState.Remove(nameof(producto.Imagen));
        ModelState.Remove(nameof(producto.IdMarcaNavigation));
        ModelState.Remove(nameof(producto.IdTipoProductoNavigation));

        if (!ModelState.IsValid)
            return Json(new { ok = false, errores = ObtenerErrores() });

        if (imagenFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await imagenFile.CopyToAsync(ms);
            producto.Imagen = ms.ToArray();
        }

        var creado = await productosRepository.CrearAsync(producto);
        var completo = await productosRepository.ObtenerPorIdAsync(creado.IdProducto);

        return Json(new
        {
            ok = true,
            mensaje = $"Producto «{creado.NombreProducto}» creado correctamente.",
            producto = MapFila(completo!)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarAjax(int id, ProProductos producto, IFormFile? imagenFile)
    {
        if (id != producto.IdProducto) return BadRequest();

        ModelState.Remove(nameof(producto.Imagen));
        ModelState.Remove(nameof(producto.IdMarcaNavigation));
        ModelState.Remove(nameof(producto.IdTipoProductoNavigation));

        if (!ModelState.IsValid)
            return Json(new { ok = false, errores = ObtenerErrores() });

        if (imagenFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await imagenFile.CopyToAsync(ms);
            producto.Imagen = ms.ToArray();
        }
        else
        {
            var existente = await productosRepository.ObtenerPorIdAsync(id);
            producto.Imagen = existente?.Imagen;
        }

        await productosRepository.ActualizarAsync(producto);
        var completo = await productosRepository.ObtenerPorIdAsync(id);

        return Json(new
        {
            ok = true,
            mensaje = $"Producto «{producto.NombreProducto}» actualizado correctamente.",
            producto = MapFila(completo!)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAjax(int id)
    {
        var ok = await productosRepository.EliminarAsync(id);
        return Json(new
        {
            ok,
            mensaje = ok ? "Producto eliminado correctamente." : "No se encontró el producto."
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task CargarSelectListsAsync(int idMarca = 0, int idTipo = 0)
    {
        var marcas = await productosRepository.ObtenerMarcasAsync();
        var tipos = await productosRepository.ObtenerTiposProductosAsync();
        ViewBag.Marcas = new SelectList(marcas, "IdMarca", "NombreMarca", idMarca);
        ViewBag.Tipos = new SelectList(tipos, "IdTipoProducto", "NombreTipoProducto", idTipo);
    }

    private Dictionary<string, string> ObtenerErrores()
        => ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors[0].ErrorMessage);

    private static object MapFila(ProProductos p) => new
    {
        p.IdProducto,
        p.NombreProducto,
        descripcion = p.Descripción ?? "",
        nombreTipo = p.IdTipoProductoNavigation?.NombreTipoProducto ?? "—",
        nombreMarca = p.IdMarcaNavigation?.NombreMarca ?? "—",
        p.Precio,
        p.Stock,
        p.Estado,
        tieneImagen = p.Imagen is { Length: > 0 },
        fechaIngreso = p.FechaIngreso.ToString("dd/MM/yyyy")
    };
}
