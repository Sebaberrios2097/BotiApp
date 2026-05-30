using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BotiApp.Areas.Productos.Controllers;

[Area("Productos")]
[Authorize(Policy = "SoloAdmin")]
public class ProductosController(
    IProductosRepository productosRepository,
    IOfertasRepository ofertasRepository,
    IPromocionesRepository promocionesRepository) : Controller
{
    // ── Index ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
        => View((await productosRepository.ObtenerTodosAsync()).Where(p => p.IdProducto != 0));

    // ── Imagen ──────────────────────────────────────────────────────────────
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEstadoAjax(int id)
    {
        var producto = await productosRepository.ObtenerPorIdAsync(id);
        if (producto is null) return Json(new { ok = false, mensaje = "Producto no encontrado." });

        // Si está activo y se va a desactivar, verificar si está en promociones activas
        if (producto.Estado)
        {
            var promos = (await ofertasRepository.ObtenerPromosActivasPorProductoAsync(id)).ToList();
            if (promos.Count > 0)
            {
                return Json(new
                {
                    ok = false,
                    requiereConfirmacion = true,
                    mensaje = $"El producto «{producto.NombreProducto}» pertenece a {promos.Count} promoción(es) activa(s).",
                    promociones = promos.Select(p => new { id = p.IdPromocion, nombre = p.Nombre })
                });
            }
        }

        var ok = await productosRepository.ToggleEstadoAsync(id);
        if (!ok) return Json(new { ok = false, mensaje = "Producto no encontrado." });

        var p2 = await productosRepository.ObtenerPorIdAsync(id);
        return Json(new
        {
            ok = true,
            mensaje = p2!.Estado ? "Producto activado." : "Producto desactivado.",
            estado = p2.Estado
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarDesactivarAjax(int id, bool desactivarPromociones)
    {
        var ok = await productosRepository.ToggleEstadoAsync(id);
        if (!ok) return Json(new { ok = false, mensaje = "Producto no encontrado." });

        if (desactivarPromociones)
        {
            var promos = await ofertasRepository.ObtenerPromosActivasPorProductoAsync(id);
            foreach (var promo in promos)
                await promocionesRepository.ToggleEstadoAsync(promo.IdPromocion);
        }

        var p = await productosRepository.ObtenerPorIdAsync(id);
        return Json(new
        {
            ok = true,
            mensaje = desactivarPromociones
                ? "Producto y sus promociones desactivados."
                : "Producto desactivado.",
            estado = p!.Estado
        });
    }

    // ── Retornables ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> TabRetornables()
    {
        var retornables = await productosRepository.ObtenerRetornablesAsync();
        var productos = await productosRepository.ObtenerTodosAsync();
        var idsRetornables = retornables.Select(r => r.IdProducto).ToHashSet();
        ViewBag.ProductosDisponibles = productos
            .Where(p => p.IdProducto != 0 && !idsRetornables.Contains(p.IdProducto) && p.Estado)
            .OrderBy(p => p.NombreProducto)
            .ToList();
        return PartialView("_TabRetornables", retornables);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarRetornableAjax(int idProducto, int valorEnvase, bool soloEfectivo)
    {
        if (idProducto <= 0 || valorEnvase <= 0)
            return Json(new { ok = false, mensaje = "Datos inválidos." });

        var retornable = new ProProductosRetornables
        {
            IdProducto = idProducto,
            ValorEnvase = valorEnvase,
            SoloEfectivo = soloEfectivo
        };

        var creado = await productosRepository.AgregarRetornableAsync(retornable);
        var completo = (await productosRepository.ObtenerRetornablesAsync())
            .FirstOrDefault(r => r.IdProducto == idProducto);

        return Json(new
        {
            ok = true,
            mensaje = "Producto retornable agregado.",
            retornable = new
            {
                completo!.IdProductoRetornable,
                completo.IdProducto,
                nombreProducto = completo.IdProductoNavigation.NombreProducto,
                nombreMarca = completo.IdProductoNavigation.IdMarcaNavigation?.NombreMarca ?? "—",
                completo.ValorEnvase,
                completo.SoloEfectivo
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarRetornableAjax(int idProducto)
    {
        var ok = await productosRepository.EliminarRetornableAsync(idProducto);
        return Json(new
        {
            ok,
            mensaje = ok ? "Producto retornable eliminado." : "No se encontró el retornable."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearMarcaAjax(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Json(new { ok = false, mensaje = "El nombre de la marca es requerido." });

        var marca = new ProMarcas { NombreMarca = nombre.Trim(), Estado = true };
        try
        {
            var creada = await productosRepository.CrearMarcaAsync(marca);
            return Json(new { ok = true, id = creada.IdMarca, nombre = creada.NombreMarca });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, mensaje = "Error al crear la marca: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTipoProductoAjax(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Json(new { ok = false, mensaje = "El nombre del tipo de producto es requerido." });

        var tipo = new ProTiposProductos { NombreTipoProducto = nombre.Trim() };
        try
        {
            var creado = await productosRepository.CrearTipoProductoAsync(tipo);
            return Json(new { ok = true, id = creado.IdTipoProducto, nombre = creado.NombreTipoProducto });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, mensaje = "Error al crear el tipo de producto: " + ex.Message });
        }
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
