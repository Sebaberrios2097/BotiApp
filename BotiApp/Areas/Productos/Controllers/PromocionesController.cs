using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotiApp.Areas.Productos.Controllers;

[Area("Productos")]
[Authorize(Policy = "SoloAdmin")]
public class PromocionesController(IPromocionesRepository promoRepo) : Controller
{
    // ── Index ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
        => View(await promoRepo.ObtenerTodasAsync());

    // ── Partials ─────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ModalCrear()
    {
        ViewBag.UltimasPromos = await promoRepo.ObtenerUltimasAsync(5);
        return PartialView("_ModalCrear", new ProPromocion
        {
            Estado = true,
            FechaInicio = DateTime.Today
        });
    }

    [HttpGet]
    public async Task<IActionResult> ModalBuscarProducto(int idPromocion, int? idGrupo)
    {
        var promo = await promoRepo.ObtenerPorIdAsync(idPromocion);
        ViewBag.IdPromocion = idPromocion;
        ViewBag.IdGrupo = idGrupo;
        ViewBag.Grupos = promo?.ProPromocionGrupo.ToList()
                              ?? new List<ProPromocionGrupo>();
        return PartialView("_ModalBuscarProducto");
    }

    // ── AJAX POST / GET ───────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAjax(ProPromocion promocion)
    {
        ModelState.Remove(nameof(promocion.ProPromocionDetalle));
        ModelState.Remove(nameof(promocion.ProPromocionGrupo));

        if (!ModelState.IsValid)
            return Json(new { ok = false, errores = ObtenerErrores() });

        var creada = await promoRepo.CrearAsync(promocion);
        return Json(new
        {
            ok = true,
            mensaje = $"Promoción «{creada.Nombre}» creada correctamente.",
            promo = MapCard(creada)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEstadoAjax(int id)
    {
        var nuevoEstado = await promoRepo.ToggleEstadoAsync(id);
        if (nuevoEstado is null)
            return Json(new { ok = false, mensaje = "Promoción no encontrada." });

        return Json(new
        {
            ok = true,
            estado = nuevoEstado,
            mensaje = nuevoEstado.Value ? "Promoción activada." : "Promoción desactivada."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarProductoAjax(
        int idPromocion, int idProducto, int cantidad = 1, int? idGrupo = null)
    {
        if (cantidad < 1) cantidad = 1;
        var detalle = await promoRepo.AgregarProductoAsync(idPromocion, idProducto, cantidad, idGrupo);
        return Json(new
        {
            ok = true,
            mensaje = $"Producto «{detalle.IdProductoNavigation.NombreProducto}» agregado.",
            detalle = MapDetalle(detalle)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarProductoAjax(int idPromocionDetalle)
    {
        var ok = await promoRepo.QuitarProductoAsync(idPromocionDetalle);
        return Json(new
        {
            ok,
            mensaje = ok ? "Producto quitado de la promoción." : "Detalle no encontrado."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearGrupoAjax(int idPromocion, string descripcion, bool esExcluyente = true)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return Json(new { ok = false, mensaje = "La descripción del grupo es requerida." });

        var grupo = await promoRepo.CrearGrupoAsync(idPromocion, descripcion.Trim(), esExcluyente);
        return Json(new
        {
            ok = true,
            mensaje = $"Grupo «{grupo.Descripcion}» creado.",
            grupo = new
            {
                grupo.IdGrupo,
                grupo.Descripcion,
                grupo.EsExcluyente
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarGrupoAjax(int idGrupo)
    {
        var ok = await promoRepo.EliminarGrupoAsync(idGrupo);
        return Json(new
        {
            ok,
            mensaje = ok ? "Grupo eliminado." : "Grupo no encontrado."
        });
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProductos(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(Array.Empty<object>());

        var productos = await promoRepo.BuscarProductosAsync(q);
        return Json(productos.Select(p => new
        {
            p.IdProducto,
            p.NombreProducto,
            nombreMarca = p.IdMarcaNavigation?.NombreMarca ?? "—",
            p.Precio,
            p.Stock,
            tieneImagen = p.Imagen is { Length: > 0 }
        }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Dictionary<string, string> ObtenerErrores()
        => ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors[0].ErrorMessage);

    private static object MapCard(ProPromocion p) => new
    {
        p.IdPromocion,
        p.Nombre,
        descripcion = p.Descripcion ?? "",
        p.PrecioPromocion,
        fechaInicio = p.FechaInicio.ToString("dd/MM/yyyy"),
        fechaFin = p.FechaFin?.ToString("dd/MM/yyyy") ?? "",
        p.Estado,
        vigente = EsVigente(p),
        totalProductos = p.ProPromocionDetalle.Count
    };

    private static object MapDetalle(ProPromocionDetalle d) => new
    {
        d.IdPromocionDetalle,
        d.IdProducto,
        d.Cantidad,
        d.IdGrupo,
        nombreGrupo = d.IdGrupoNavigation?.Descripcion,
        esExcluyente = d.IdGrupoNavigation?.EsExcluyente,
        nombreProducto = d.IdProductoNavigation.NombreProducto,
        nombreMarca = d.IdProductoNavigation.IdMarcaNavigation?.NombreMarca ?? "—",
        precio = d.IdProductoNavigation.Precio,
        stock = d.IdProductoNavigation.Stock,
        tieneImagen = d.IdProductoNavigation.Imagen is { Length: > 0 }
    };

    internal static bool EsVigente(ProPromocion p)
    {
        if (!p.Estado) return false;
        var hoy = DateTime.Today;
        if (p.FechaInicio > hoy) return false;
        if (p.FechaFin.HasValue && p.FechaFin.Value.Date < hoy) return false;
        return true;
    }
}
