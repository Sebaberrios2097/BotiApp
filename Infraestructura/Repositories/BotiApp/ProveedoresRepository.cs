using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class ProveedoresRepository(BotiAppContext context) : IProveedoresRepository
{
    public async Task<IEnumerable<ProProveedores>> ObtenerTodosAsync()
        => await context.ProProveedores
            .Include(p => p.ProProveedoresProductos.Where(pp => pp.Estado))
            .OrderBy(p => p.NombreProveedor)
            .AsNoTracking()
            .ToListAsync();

    public async Task<ProProveedores?> ObtenerPorIdAsync(int id)
        => await context.ProProveedores
            .Include(p => p.ProProveedoresProductos.Where(pp => pp.Estado))
                .ThenInclude(pp => pp.IdProductoNavigation)
                    .ThenInclude(pr => pr.IdTipoProductoNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProveedor == id);

    public async Task<ProProveedores> CrearAsync(ProProveedores proveedor)
    {
        proveedor.Estado = true;
        context.ProProveedores.Add(proveedor);
        await context.SaveChangesAsync();
        return proveedor;
    }

    public async Task<ProProveedores> ActualizarAsync(ProProveedores proveedor)
    {
        context.ProProveedores.Update(proveedor);
        await context.SaveChangesAsync();
        return proveedor;
    }

    public async Task<bool> ToggleEstadoAsync(int id)
    {
        var proveedor = await context.ProProveedores.FindAsync(id);
        if (proveedor is null) return false;
        proveedor.Estado = !proveedor.Estado;
        await context.SaveChangesAsync();
        return true;
    }

    // ── Productos asociados ────────────────────────────────────────────────────

    public async Task<IEnumerable<ProProveedoresProductos>> ObtenerProductosAsync(int idProveedor)
        => await context.ProProveedoresProductos
            .Include(pp => pp.IdProductoNavigation)
                .ThenInclude(p => p.IdTipoProductoNavigation)
            .Include(pp => pp.IdProductoNavigation)
                .ThenInclude(p => p.IdMarcaNavigation)
            .Where(pp => pp.IdProveedor == idProveedor && pp.Estado)
            .OrderBy(pp => pp.IdProductoNavigation.NombreProducto)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<ProProductos>> ObtenerProductosDisponiblesAsync(int idProveedor)
    {
        var yaAsociados = await context.ProProveedoresProductos
            .Where(pp => pp.IdProveedor == idProveedor && pp.Estado)
            .Select(pp => pp.IdProducto)
            .ToListAsync();

        return await context.ProProductos
            .Include(p => p.IdTipoProductoNavigation)
            .Include(p => p.IdMarcaNavigation)
            .Where(p => p.Estado && !yaAsociados.Contains(p.IdProducto))
            .OrderBy(p => p.NombreProducto)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ProProveedoresProductos> AsociarProductoAsync(int idProveedor, int idProducto, int precioProveedor = 0)
    {
        // Si existe pero estaba desactivado, reactivar
        var existente = await context.ProProveedoresProductos
            .FirstOrDefaultAsync(pp => pp.IdProveedor == idProveedor && pp.IdProducto == idProducto);

        if (existente is not null)
        {
            existente.Estado = true;
            existente.PrecioProveedor = precioProveedor;
            existente.FechaModificacion = DateTime.Now;
            await context.SaveChangesAsync();
            return existente;
        }

        var nuevo = new ProProveedoresProductos
        {
            IdProveedor = idProveedor,
            IdProducto = idProducto,
            Estado = true,
            PrecioProveedor = precioProveedor,
            FechaModificacion = DateTime.Now
        };
        context.ProProveedoresProductos.Add(nuevo);
        await context.SaveChangesAsync();
        return nuevo;
    }

    public async Task<bool> DesasociarProductoAsync(int idProveedorProducto)
    {
        var rel = await context.ProProveedoresProductos.FindAsync(idProveedorProducto);
        if (rel is null) return false;
        rel.Estado = false;
        rel.FechaModificacion = DateTime.Now;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActualizarPrecioProductoAsync(int idProveedorProducto, int precioProveedor)
    {
        var rel = await context.ProProveedoresProductos.FindAsync(idProveedorProducto);
        if (rel is null) return false;
        rel.PrecioProveedor = precioProveedor;
        rel.FechaModificacion = DateTime.Now;
        await context.SaveChangesAsync();
        return true;
    }
}
