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
    public async Task<IActionResult> ModalCrear(string? codigo = null)
    {
        await CargarSelectListsAsync();
        ViewBag.UltimosProductos = await productosRepository.ObtenerUltimosIngresadosAsync(5);
        ViewBag.CodigoInicial = codigo;
        ViewBag.ProductosUnidadDisponibles = await ObtenerProductosUnidadDisponiblesAsync(excluirId: 0);
        return PartialView("_ModalCrear", new ProProductos { Estado = true, FechaIngreso = DateTime.Now });
    }

    [HttpGet]
    public async Task<IActionResult> ModalEditar(int id)
    {
        var producto = await productosRepository.ObtenerPorIdAsync(id);
        if (producto is null) return NotFound();

        await CargarSelectListsAsync(producto.IdMarca, producto.IdTipoProducto);
        ViewBag.Auditoria = await productosRepository.ObtenerAuditoriaAsync(id);
        var packActual = await productosRepository.ObtenerPackPorProductoAsync(id);
        ViewBag.PackActual = packActual;
        ViewBag.ProductosUnidadDisponibles = await ObtenerProductosUnidadDisponiblesAsync(excluirId: id, incluirAdicionalId: packActual?.IdProductoUnidad);
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
    public async Task<IActionResult> CrearAjax(ProProductos producto, IFormFile? imagenFile,
        bool esPack = false, int idProductoUnidad = 0, int cantidadUnidades = 0)
    {
        ModelState.Remove(nameof(producto.Imagen));
        ModelState.Remove(nameof(producto.IdMarcaNavigation));
        ModelState.Remove(nameof(producto.IdTipoProductoNavigation));

        if (!ModelState.IsValid)
            return Json(new { ok = false, errores = ObtenerErrores() });

        if (esPack)
        {
            // idProductoPack va en 0: el producto todavía no existe
            var error = await ValidarPackAsync(0, idProductoUnidad, cantidadUnidades);
            if (error is not null)
                return Json(new { ok = false, mensaje = error });
        }

        if (imagenFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await imagenFile.CopyToAsync(ms);
            producto.Imagen = ms.ToArray();
        }

        var creado = await productosRepository.CrearAsync(producto);
        var completo = await productosRepository.ObtenerPorIdAsync(creado.IdProducto);

        if (esPack)
        {
            await productosRepository.UpsertPackAsync(new ProProductoPack
            {
                IdProductoPackProducto = creado.IdProducto,
                IdProductoUnidad = idProductoUnidad,
                CantidadUnidades = cantidadUnidades
            });
        }

        return Json(new
        {
            ok = true,
            mensaje = $"Producto «{creado.NombreProducto}» creado correctamente.",
            producto = MapFila(completo!)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarAjax(int id, ProProductos producto, IFormFile? imagenFile,
        bool esPack = false, int idProductoUnidad = 0, int cantidadUnidades = 0)
    {
        if (id != producto.IdProducto) return BadRequest();

        ModelState.Remove(nameof(producto.Imagen));
        ModelState.Remove(nameof(producto.IdMarcaNavigation));
        ModelState.Remove(nameof(producto.IdTipoProductoNavigation));

        if (!ModelState.IsValid)
            return Json(new { ok = false, errores = ObtenerErrores() });

        var existente = await productosRepository.ObtenerPorIdAsync(id);
        var packPrevio = await productosRepository.ObtenerPackPorProductoAsync(id);

        if (esPack)
        {
            var error = await ValidarPackAsync(id, idProductoUnidad, cantidadUnidades);
            if (error is not null)
                return Json(new { ok = false, mensaje = error });
        }

        if (imagenFile is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await imagenFile.CopyToAsync(ms);
            producto.Imagen = ms.ToArray();
        }
        else
        {
            producto.Imagen = existente?.Imagen;
        }

        if (existente != null)
            producto.FechaIngreso = existente.FechaIngreso;

        await productosRepository.ActualizarAsync(producto);

        if (esPack)
        {
            await productosRepository.UpsertPackAsync(new ProProductoPack
            {
                IdProductoPackProducto = id,
                IdProductoUnidad = idProductoUnidad,
                CantidadUnidades = cantidadUnidades
            });
        }
        else if (packPrevio is not null)
        {
            await productosRepository.EliminarPackPorProductoAsync(id);
        }

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

    // ── Control de Stock / Inventario ───────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Stock()
    {
        await CargarSelectListsAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Buscar(string filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro))
            return Json(Array.Empty<object>());

        var productos = await productosRepository.BuscarAsync(filtro);
        var resultado = productos.Select(MapFila);
        return Json(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> ExisteCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return Json(new { existe = false });

        var existe = await productosRepository.ExisteCodigoAsync(codigo);
        return Json(new { existe });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task CargarSelectListsAsync(int idMarca = 0, int idTipo = 0)
    {
        var marcas = await productosRepository.ObtenerMarcasAsync();
        var tipos = await productosRepository.ObtenerTiposProductosAsync();
        ViewBag.Marcas = new SelectList(marcas, "IdMarca", "NombreMarca", idMarca);
        ViewBag.Tipos = new SelectList(tipos, "IdTipoProducto", "NombreTipoProducto", idTipo);
    }

    /// <summary>
    /// Devuelve los productos que pueden ser elegidos como "unidad base" de un pack:
    /// activos y distintos de IdProducto (para evitar autoreferencia).
    /// Una misma unidad SÍ puede alimentar varios packs (uno de 6 y otro de 12 del
    /// mismo producto), por eso no se excluyen las unidades ya usadas. Lo que sí se
    /// excluye es un producto que ya es pack: encadenar packs rompería el cálculo
    /// de stock, que deriva cada pack de su unidad.
    /// Si <paramref name="incluirAdicionalId"/> viene (caso edición), fuerza a que ese
    /// producto aparezca aunque esté inactivo o sea la unidad actualmente seleccionada.
    /// </summary>
    /// <summary>
    /// Reglas de un pack, validadas en servidor (la lista del modal solo las sugiere).
    /// Devuelve el mensaje de error, o null si la definición es válida.
    /// Varios packs pueden compartir la misma unidad base siempre que sean de distinto
    /// tamaño; lo que no se permite es encadenar packs ni duplicar un tamaño.
    /// </summary>
    private async Task<string?> ValidarPackAsync(int idProductoPack, int idProductoUnidad, int cantidadUnidades)
    {
        if (idProductoUnidad <= 0 || cantidadUnidades <= 1)
            return "Para definir un pack debe seleccionar una unidad y una cantidad mayor a 1.";

        if (idProductoUnidad == idProductoPack)
            return "Un producto no puede ser pack de sí mismo.";

        var idsQueSonPack = await productosRepository.ObtenerIdsProductosQueSonPackAsync();
        if (idsQueSonPack.Contains(idProductoUnidad))
            return "La unidad base no puede ser a su vez un pack: el stock se calcula desde la unidad y no se propaga en cadena.";

        // El producto que se está definiendo no puede ser ya la unidad de otro pack
        if (idProductoPack > 0 && (await productosRepository.ObtenerPacksPorUnidadAsync(idProductoPack)).Count > 0)
            return "Este producto ya es la unidad base de otro pack, por lo que no puede convertirse en pack.";

        // Dos packs del mismo tamaño sobre la misma unidad serían indistinguibles
        var packsDeLaUnidad = await productosRepository.ObtenerPacksPorUnidadAsync(idProductoUnidad);
        var duplicado = packsDeLaUnidad.FirstOrDefault(p => p.CantidadUnidades == cantidadUnidades
                                                            && p.IdProductoPackProducto != idProductoPack);
        if (duplicado is not null)
            return $"Ya existe un pack de {cantidadUnidades} unidades para esa unidad base.";

        return null;
    }

    private async Task<List<ProProductos>> ObtenerProductosUnidadDisponiblesAsync(int excluirId, int? incluirAdicionalId = null)
    {
        var productos = await productosRepository.ObtenerTodosAsync();
        var idsQueSonPack = await productosRepository.ObtenerIdsProductosQueSonPackAsync();
        var lista = productos
            .Where(p => p.IdProducto != 0
                        && p.IdProducto != excluirId
                        && p.Estado
                        && !idsQueSonPack.Contains(p.IdProducto))
            .ToList();
        if (incluirAdicionalId is int idExtra && idExtra > 0 && idExtra != excluirId && !lista.Any(p => p.IdProducto == idExtra))
        {
            var extra = productos.FirstOrDefault(p => p.IdProducto == idExtra);
            if (extra is not null) lista.Add(extra);
        }
        return lista.OrderBy(p => p.NombreProducto).ToList();
    }

    private Dictionary<string, string> ObtenerErrores()
        => ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors[0].ErrorMessage);

    private static object MapFila(ProProductos p) => new
    {
        p.IdProducto,
        p.NombreProducto,
        codigo = p.Codigo ?? "",
        descripcion = p.Descripción ?? "",
        nombreTipo = p.IdTipoProductoNavigation?.NombreTipoProducto ?? "—",
        nombreMarca = p.IdMarcaNavigation?.NombreMarca ?? "—",
        idTipoProducto = p.IdTipoProducto,
        idMarca = p.IdMarca,
        p.Precio,
        p.Stock,
        p.Estado,
        tieneImagen = p.Imagen is { Length: > 0 },
        fechaIngreso = p.FechaIngreso.ToString("dd/MM/yyyy")
    };
}
