using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class ProductosRepository(BotiAppContext context) : IProductosRepository
{
    public async Task<IEnumerable<ProProductos>> ObtenerTodosAsync()
        => await context.ProProductos
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .Include(p => p.ProProductosRetornables)
            .OrderBy(p => p.NombreProducto)
            .ToListAsync();

    public async Task<ProProductos?> ObtenerPorIdAsync(int id)
        => await context.ProProductos
            .AsNoTracking()  // 👈 evita el tracking
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .FirstOrDefaultAsync(p => p.IdProducto == id);

    public async Task<ProProductos> CrearAsync(ProProductos producto)
    {
        producto.FechaIngreso = DateTime.Now;
        context.ProProductos.Add(producto);
        await context.SaveChangesAsync();
        return producto;
    }

    public async Task<ProProductos> ActualizarAsync(ProProductos producto)
    {
        context.ProProductos.Update(producto);
        await context.SaveChangesAsync();
        return producto;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var producto = await context.ProProductos.FindAsync(id);
        if (producto is null) return false;
        context.ProProductos.Remove(producto);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleEstadoAsync(int id)
    {
        var producto = await context.ProProductos.FindAsync(id);
        if (producto is null) return false;
        producto.Estado = !producto.Estado;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ProMarcas>> ObtenerMarcasAsync()
        => await context.ProMarcas
            .Where(m => m.Estado)
            .OrderBy(m => m.NombreMarca)
            .ToListAsync();

    public async Task<IEnumerable<ProTiposProductos>> ObtenerTiposProductosAsync()
        => await context.ProTiposProductos
            .OrderBy(t => t.NombreTipoProducto)
            .ToListAsync();

    public async Task<IEnumerable<AudProProductos>> ObtenerAuditoriaAsync(int idProducto, int top = 6)
        => await context.AudProProductos
            .Where(a => a.IdProducto == idProducto)
            .OrderByDescending(a => a.FechaModificacion)
            .Take(top)
            .ToListAsync();

    public async Task<IEnumerable<ProProductos>> ObtenerUltimosIngresadosAsync(int top = 5)
        => await context.ProProductos
            .Include(p => p.IdMarcaNavigation)
            .OrderByDescending(p => p.FechaIngreso)
            .Take(top)
            .ToListAsync();

    // ── Retornables ────────────────────────────────────────────────────────
    public async Task<IEnumerable<ProProductosRetornables>> ObtenerRetornablesAsync()
        => await context.ProProductosRetornables
            .Include(r => r.IdProductoNavigation)
                .ThenInclude(p => p.IdMarcaNavigation)
            .Include(r => r.IdProductoNavigation)
                .ThenInclude(p => p.IdTipoProductoNavigation)
            .OrderBy(r => r.IdProductoNavigation.NombreProducto)
            .ToListAsync();

    public async Task<ProProductosRetornables> AgregarRetornableAsync(ProProductosRetornables retornable)
    {
        context.ProProductosRetornables.Add(retornable);
        await context.SaveChangesAsync();
        return retornable;
    }

    public async Task<bool> EliminarRetornableAsync(int idProducto)
    {
        var r = await context.ProProductosRetornables
            .FirstOrDefaultAsync(x => x.IdProducto == idProducto);
        if (r is null) return false;
        context.ProProductosRetornables.Remove(r);
        await context.SaveChangesAsync();
        return true;
    }
}
