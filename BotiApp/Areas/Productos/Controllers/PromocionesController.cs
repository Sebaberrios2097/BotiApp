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

    // ── Edición en vista completa ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var promo = await promoRepo.ObtenerPorIdAsync(id);
        if (promo is null) return NotFound();
        return View(promo);
    }

    /// <summary>
    /// Devuelve solo el bloque de productos y grupos de la promoción. La vista de
    /// edición lo recarga tras cada cambio para que contadores, estados vacíos y
    /// resumen de precios queden siempre consistentes sin parchear el DOM a mano.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ContenidoPromo(int id)
    {
        var promo = await promoRepo.ObtenerPorIdAsync(id);
        if (promo is null) return NotFound();
        return PartialView("_ContenidoPromo", promo);
    }

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
        if (!ValidarPromocion(promocion))
            return Json(new { ok = false, errores = ObtenerErrores() });

        var creada = await promoRepo.CrearAsync(promocion);
        return Json(new
        {
            ok = true,
            mensaje = $"Promoción «{creada.Nombre}» creada correctamente.",
            idPromocion = creada.IdPromocion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarAjax(ProPromocion promocion)
    {
        if (!ValidarPromocion(promocion))
            return Json(new { ok = false, errores = ObtenerErrores() });

        var actualizada = await promoRepo.ActualizarAsync(promocion);
        if (actualizada is null)
            return Json(new { ok = false, mensaje = "Promoción no encontrada." });

        return Json(new
        {
            ok = true,
            mensaje = "Cambios guardados correctamente.",
            promo = new
            {
                actualizada.IdPromocion,
                actualizada.Nombre,
                actualizada.PrecioPromocion,
                actualizada.Estado,
                estadoClave = EstadoClave(actualizada),
                estadoTexto = EstadoTexto(actualizada)
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEstadoAjax(int id)
    {
        var promo = await promoRepo.ToggleEstadoAsync(id);
        if (promo is null)
            return Json(new { ok = false, mensaje = "Promoción no encontrada." });

        return Json(new
        {
            ok = true,
            promo.Estado,
            estadoClave = EstadoClave(promo),
            estadoTexto = EstadoTexto(promo),
            mensaje = promo.Estado ? "Promoción activada." : "Promoción desactivada."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAjax(int id)
    {
        var promo = await promoRepo.ObtenerPorIdAsync(id);
        if (promo is null)
            return Json(new { ok = false, mensaje = "Promoción no encontrada." });

        var nombre = promo.Nombre;
        var ok = await promoRepo.EliminarAsync(id);
        if (!ok)
            return Json(new { ok = false, mensaje = "No se pudo eliminar la promoción." });

        return Json(new { ok = true, mensaje = $"Promoción «{nombre}» eliminada." });
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
    public async Task<IActionResult> QuitarProductoAjax(int id)
    {
        var ok = await promoRepo.QuitarProductoAsync(id);
        return Json(new
        {
            ok,
            mensaje = ok ? "Producto quitado de la promoción." : "Detalle no encontrado."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarCantidadAjax(int id, int cantidad)
    {
        if (cantidad < 1)
            return Json(new { ok = false, mensaje = "La cantidad debe ser al menos 1." });

        var detalle = await promoRepo.ActualizarCantidadAsync(id, cantidad);
        if (detalle is null)
            return Json(new { ok = false, mensaje = "Detalle no encontrado." });

        return Json(new { ok = true, mensaje = "Cantidad actualizada.", cantidad = detalle.Cantidad });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoverProductoAjax(int id, int? idGrupo)
    {
        var detalle = await promoRepo.MoverDetalleAsync(id, idGrupo);
        if (detalle is null)
            return Json(new { ok = false, mensaje = "No se pudo mover el producto." });

        return Json(new
        {
            ok = true,
            mensaje = idGrupo.HasValue
                ? "Producto movido al grupo."
                : "Producto movido a productos base."
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

    // El id viaja en el segmento de ruta, que se llama «id»: el parámetro debe
    // llamarse igual para que el binder lo tome.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarGrupoAjax(int id)
    {
        var ok = await promoRepo.EliminarGrupoAsync(id);
        return Json(new
        {
            ok,
            mensaje = ok ? "Grupo y sus productos fueron eliminados." : "Grupo no encontrado."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenombrarGrupoAjax(int idGrupo, string descripcion, bool esExcluyente)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return Json(new { ok = false, mensaje = "La descripción del grupo es requerida." });

        var grupo = await promoRepo.RenombrarGrupoAsync(idGrupo, descripcion, esExcluyente);
        if (grupo is null)
            return Json(new { ok = false, mensaje = "Grupo no encontrado." });

        return Json(new
        {
            ok = true,
            mensaje = $"Grupo «{grupo.Descripcion}» actualizado.",
            grupo = new
            {
                grupo.IdGrupo,
                grupo.Descripcion,
                grupo.EsExcluyente
            }
        });
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProductos(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(Array.Empty<object>());

        var productos = await promoRepo.BuscarProductosAsync(q);
        return Json(productos.Select(p => MapProductoListado(p)));
    }

    [HttpGet]
    public async Task<IActionResult> ListarProductos()
    {
        var productos = await promoRepo.ListarProductosActivosAsync();
        return Json(productos.Select(p => MapProductoListado(p)));
    }

    private static object MapProductoListado(ProProductos p) => new
    {
        p.IdProducto,
        p.NombreProducto,
        nombreMarca = p.IdMarcaNavigation?.NombreMarca ?? "—",
        p.Precio,
        p.Stock,
        tieneImagen = p.Imagen is { Length: > 0 }
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Valida los datos propios de la promoción (comunes a crear y editar) y deja
    /// los mensajes en ModelState para que el formulario los muestre por campo.
    /// </summary>
    private bool ValidarPromocion(ProPromocion p)
    {
        ModelState.Remove(nameof(p.ProPromocionDetalle));
        ModelState.Remove(nameof(p.ProPromocionGrupo));

        if (p.PrecioPromocion <= 0)
            ModelState.AddModelError(nameof(p.PrecioPromocion), "El precio debe ser mayor a 0.");

        if (p.FechaFin.HasValue && p.FechaFin.Value.Date < p.FechaInicio.Date)
            ModelState.AddModelError(nameof(p.FechaFin), "No puede ser anterior a la fecha de inicio.");

        return ModelState.IsValid;
    }

    private Dictionary<string, string> ObtenerErrores()
        => ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors[0].ErrorMessage);

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

    /// <summary>
    /// Estado mostrado al usuario: inactiva / próxima / vencida / vigente.
    /// Se usa como clave de badge y de filtro en el listado.
    /// </summary>
    internal static string EstadoClave(ProPromocion p)
    {
        if (!p.Estado) return "inactiva";
        var hoy = DateTime.Today;
        if (p.FechaInicio.Date > hoy) return "proxima";
        if (p.FechaFin.HasValue && p.FechaFin.Value.Date < hoy) return "vencida";
        return "vigente";
    }

    internal static string EstadoTexto(ProPromocion p) => EstadoClave(p) switch
    {
        "inactiva" => "Inactiva",
        "proxima"  => "Próxima",
        "vencida"  => "Vencida",
        _          => "Vigente"
    };

    /// <summary>
    /// Precio que pagaría el cliente comprando los productos por separado. Los grupos
    /// excluyentes hacen que el total varíe según la opción elegida, por eso se
    /// devuelve un rango: mínimo (opción más barata) y máximo (opción más cara).
    /// </summary>
    internal static (int Min, int Max) PrecioNormal(ProPromocion p)
    {
        var baseSum = p.ProPromocionDetalle
            .Where(d => d.IdGrupo == null)
            .Sum(d => d.IdProductoNavigation.Precio * d.Cantidad);

        var min = baseSum;
        var max = baseSum;

        foreach (var g in p.ProPromocionGrupo)
        {
            var items = p.ProPromocionDetalle
                .Where(d => d.IdGrupo == g.IdGrupo)
                .Select(d => d.IdProductoNavigation.Precio * d.Cantidad)
                .ToList();
            if (items.Count == 0) continue;

            if (g.EsExcluyente)
            {
                min += items.Min();
                max += items.Max();
            }
            else
            {
                var total = items.Sum();
                min += total;
                max += total;
            }
        }

        return (min, max);
    }
}
