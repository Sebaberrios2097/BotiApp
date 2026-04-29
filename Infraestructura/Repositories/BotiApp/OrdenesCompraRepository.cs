using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class OrdenesCompraRepository(BotiAppContext context) : IOrdenesCompraRepository
{
    private IQueryable<ComOrdenCompra> OrdenesConIncludes()
        => context.ComOrdenCompra
            .Include(o => o.IdProveedorNavigation)
            .Include(o => o.IdEstadoOrdenCompraNavigation)
            .Include(o => o.IdUsuarioNavigation).ThenInclude(u => u.IdEmpleadoNavigation)
            .Include(o => o.ComOrdenDetalle)
                .ThenInclude(d => d.IdProveedorProductoNavigation)
                    .ThenInclude(pp => pp.IdProductoNavigation);

    public async Task<IEnumerable<ComOrdenCompra>> ObtenerTodasAsync()
        => await OrdenesConIncludes()
            .AsNoTracking()
            .OrderByDescending(o => o.FechaSolicitud)
            .ToListAsync();

    public async Task<ComOrdenCompra?> ObtenerPorIdAsync(int id)
        => await OrdenesConIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.IdOrdenCompra == id);

    public async Task<ComOrdenCompra> CrearAsync(ComOrdenCompra orden, IEnumerable<ComOrdenDetalle> detalles)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        orden.FechaSolicitud = DateTime.Now;
        context.ComOrdenCompra.Add(orden);
        await context.SaveChangesAsync();

        foreach (var item in detalles)
        {
            item.IdOrdenCompra = orden.IdOrdenCompra;
            item.IdProveedor   = orden.IdProveedor;
            context.ComOrdenDetalle.Add(item);
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return orden;
    }

    public async Task<bool> CambiarEstadoAsync(int idOrden, int idEstado)
    {
        var orden = await context.ComOrdenCompra.FindAsync(idOrden);
        if (orden is null) return false;
        orden.IdEstadoOrdenCompra = idEstado;
        if (idEstado == 3) // Recibida
            orden.FechaLlegadaPedido = DateTime.Now;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ComEstadosOrdenCompra>> ObtenerEstadosAsync()
        => await context.ComEstadosOrdenCompra
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<ProProveedores>> ObtenerProveedoresActivosAsync()
        => await context.ProProveedores
            .Where(p => p.Estado)
            .OrderBy(p => p.NombreProveedor)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<ProProveedoresProductos>> ObtenerProductosPorProveedorAsync(
        int idProveedor, int? idCategoria = null)
    {
        var query = context.ProProveedoresProductos
            .Include(pp => pp.IdProductoNavigation)
                .ThenInclude(p => p.IdTipoProductoNavigation)
            .Include(pp => pp.IdProductoNavigation)
                .ThenInclude(p => p.IdMarcaNavigation)
            .Where(pp => pp.IdProveedor == idProveedor && pp.Estado && pp.IdProductoNavigation.Estado);

        if (idCategoria.HasValue)
            query = query.Where(pp => pp.IdProductoNavigation.IdTipoProducto == idCategoria.Value);

        return await query
            .OrderBy(pp => pp.IdProductoNavigation.NombreProducto)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<ProTiposProductos>> ObtenerCategoriasConProductosAsync(int idProveedor)
    {
        var idsCategoria = await context.ProProveedoresProductos
            .Where(pp => pp.IdProveedor == idProveedor && pp.Estado && pp.IdProductoNavigation.Estado)
            .Select(pp => pp.IdProductoNavigation.IdTipoProducto)
            .Distinct()
            .ToListAsync();

        return await context.ProTiposProductos
            .Where(t => idsCategoria.Contains(t.IdTipoProducto))
            .OrderBy(t => t.NombreTipoProducto)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ActualizarPreciosOrdenAsync(int idOrden, bool incluyeIva,
        IEnumerable<(int idOrdenDetalle, int precioUnitario)> precios)
    {
        var orden = await context.ComOrdenCompra
            .Include(o => o.ComOrdenDetalle)
            .FirstOrDefaultAsync(o => o.IdOrdenCompra == idOrden);
        if (orden is null) return false;

        orden.IncluyeIva = incluyeIva;

        foreach (var (idDetalle, precio) in precios)
        {
            var det = orden.ComOrdenDetalle.FirstOrDefault(d => d.IdOrdenDetalle == idDetalle);
            if (det is null) continue;
            det.PrecioUnitario = precio;
            det.Subtotal       = det.Cantidad * precio;
        }

        orden.MontoTotal = orden.ComOrdenDetalle.Sum(d => d.Subtotal);
        await context.SaveChangesAsync();
        return true;
    }
}
