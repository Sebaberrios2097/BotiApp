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
            .OrderBy(p => p.NombreProducto)
            .ToListAsync();

    public async Task<ProProductos?> ObtenerPorIdAsync(int id)
        => await context.ProProductos
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
}