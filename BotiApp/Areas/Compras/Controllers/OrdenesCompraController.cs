using BotiApp.Helpers;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotiApp.Areas.Compras.Controllers;

[Area("Compras")]
[Authorize(Policy = "SoloAdmin")]
public class OrdenesCompraController(IOrdenesCompraRepository repo) : Controller
{
    // ── Lista ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Lista()
    {
        ViewBag.Estados = await repo.ObtenerEstadosAsync();
        return View(await repo.ObtenerTodasAsync());
    }

    // ── Crear (GET) ───────────────────────────────────────────────────────────
    public async Task<IActionResult> Crear()
    {
        ViewBag.Proveedores = await repo.ObtenerProveedoresActivosAsync();
        return View();
    }

    // ── AJAX: categorías por proveedor ────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Categorias(int idProveedor)
    {
        var cats = await repo.ObtenerCategoriasConProductosAsync(idProveedor);
        return Json(cats.Select(c => new { c.IdTipoProducto, c.NombreTipoProducto }));
    }

    // ── AJAX: datos de orden para editar precios ──────────────────────────────
    [HttpGet]
    public async Task<IActionResult> OrdenParaEditar(int id)
    {
        var orden = await repo.ObtenerPorIdAsync(id);
        if (orden is null) return NotFound();

        return Json(new
        {
            idOrdenCompra = orden.IdOrdenCompra,
            idEstado      = orden.IdEstadoOrdenCompra,
            idProveedor   = orden.IdProveedor,
            proveedor     = orden.IdProveedorNavigation.NombreProveedor,
            incluyeIva    = orden.IncluyeIva,
            detalles      = orden.ComOrdenDetalle.Select(d => new
            {
                idOrdenDetalle = d.IdOrdenDetalle,
                producto       = d.IdProveedorProductoNavigation?.IdProductoNavigation?.NombreProducto ?? "—",
                cantidad       = d.Cantidad,
                precioUnitario = d.PrecioUnitario
            })
        });
    }

    // ── Actualizar precios de orden ───────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarPrecios([FromBody] EditarPreciosDto dto)
    {
        if (dto.Detalles is null || !dto.Detalles.Any())
            return Json(new { ok = false, mensaje = "Sin detalles." });

        var ok = await repo.ActualizarPreciosOrdenAsync(
            dto.IdOrdenCompra,
            dto.IncluyeIva,
            dto.Detalles.Select(d => (d.IdOrdenDetalle, d.PrecioUnitario)));

        return Json(new { ok, mensaje = ok ? "Precios registrados correctamente." : "Orden no encontrada." });
    }

    // ── AJAX: productos por proveedor + categoría ─────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Productos(int idProveedor, int? idCategoria)
    {
        var items = await repo.ObtenerProductosPorProveedorAsync(idProveedor, idCategoria);
        return Json(items.Select(pp => new
        {
            idProveedorProducto = pp.IdProveedorProducto,
            idProducto          = pp.IdProductoNavigation.IdProducto,
            nombre              = pp.IdProductoNavigation.NombreProducto,
            categoria           = pp.IdProductoNavigation.IdTipoProductoNavigation.NombreTipoProducto,
            precioProveedor     = pp.PrecioProveedor,
            stock               = pp.IdProductoNavigation.Stock
        }));
    }

    // ── Crear (POST) ──────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearOrden([FromBody] CrearOrdenDto dto)
    {
        if (dto.Detalles is null || !dto.Detalles.Any())
            return Json(new { ok = false, mensaje = "Debe agregar al menos un producto." });

        var idUsuario = ClaimHelper.GetIdUsuario(User);

        var orden = new ComOrdenCompra
        {
            IdProveedor           = dto.IdProveedor,
            IdUsuario             = idUsuario,
            IdEstadoOrdenCompra   = 1, // Pendiente
            CantidadProductos     = dto.Detalles.Sum(d => d.Cantidad),
            MontoTotal            = dto.Detalles.Sum(d => d.Subtotal)
        };

        var detalles = dto.Detalles.Select(d => new ComOrdenDetalle
        {
            IdProveedorProducto = d.IdProveedorProducto,
            Cantidad            = d.Cantidad,
            PrecioUnitario      = d.PrecioUnitario,
            Subtotal            = d.Subtotal
        }).ToList();

        var creada = await repo.CrearAsync(orden, detalles);
        return Json(new { ok = true, idOrden = creada.IdOrdenCompra, mensaje = "Orden de compra creada correctamente." });
    }

    // ── Replicar orden ─────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplicarOrden(int id)
    {
        var idUsuario = ClaimHelper.GetIdUsuario(User);
        var nueva = await repo.ReplicarOrdenAsync(id, idUsuario);
        if (nueva is null)
            return Json(new { ok = false, mensaje = "Orden no encontrada." });
        return Json(new { ok = true, idOrden = nueva.IdOrdenCompra, mensaje = $"Orden #{nueva.IdOrdenCompra} creada correctamente." });
    }

    // ── Cambiar estado ────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, int idEstado)
    {
        var ok = await repo.CambiarEstadoAsync(id, idEstado);
        return Json(new { ok, mensaje = ok ? "Estado actualizado." : "Orden no encontrada." });
    }

    // ── Agregar producto a orden ─────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarDetalle([FromBody] AgregarDetalleDto dto)
    {
        var det = await repo.AgregarDetalleOrdenAsync(
            dto.IdOrdenCompra, dto.IdProveedorProducto, dto.Cantidad, dto.PrecioUnitario);
        if (det is null)
            return Json(new { ok = false, mensaje = "No se pudo agregar el producto. Verifique que la orden esté en estado Generada." });

        return Json(new
        {
            ok             = true,
            idOrdenDetalle = det.IdOrdenDetalle,
            producto       = det.IdProveedorProductoNavigation?.IdProductoNavigation?.NombreProducto ?? "—",
            cantidad       = det.Cantidad,
            precioUnitario = det.PrecioUnitario,
            subtotal       = det.Subtotal,
            mensaje        = "Producto agregado correctamente."
        });
    }
    // ── Actualizar cantidad de un detalle ─────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarCantidad(int idOrden, int idDetalle, int cantidad)
    {
        var ok = await repo.ActualizarCantidadDetalleAsync(idOrden, idDetalle, cantidad);
        return Json(new { ok, mensaje = ok ? "Cantidad actualizada." : "No se pudo actualizar. Verifique que la orden esté en estado Generada." });
    }
    // ── Eliminar producto de orden ───────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarDetalle(int idOrden, int idDetalle)
    {
        var ok = await repo.EliminarDetalleOrdenAsync(idOrden, idDetalle);
        return Json(new { ok, mensaje = ok ? "Producto eliminado." : "No se pudo eliminar el producto." });
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public class CrearOrdenDto
{
    public int IdProveedor { get; set; }
    public List<DetalleOrdenDto>? Detalles { get; set; }
}

public class DetalleOrdenDto
{
    public int IdProveedorProducto { get; set; }
    public int Cantidad            { get; set; }
    public int PrecioUnitario      { get; set; }
    public int Subtotal            { get; set; }
}

public class EditarPreciosDto
{
    public int IdOrdenCompra { get; set; }
    public bool IncluyeIva   { get; set; }
    public List<EditarDetalleDto>? Detalles { get; set; }
}

public class EditarDetalleDto
{
    public int IdOrdenDetalle  { get; set; }
    public int PrecioUnitario  { get; set; }
}

public class AgregarDetalleDto
{
    public int IdOrdenCompra       { get; set; }
    public int IdProveedorProducto { get; set; }
    public int Cantidad            { get; set; }
    public int PrecioUnitario      { get; set; }
}
