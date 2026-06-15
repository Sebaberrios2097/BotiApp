using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class ProductosRepository(BotiAppContext context) : IProductosRepository
{
    public async Task<IEnumerable<ProProductos>> ObtenerTodosAsync()
    {
        var list = await context.ProProductos
            .AsNoTracking()
            .OrderBy(p => p.NombreProducto)
            .Select(p => new
            {
                p.IdProducto,
                p.IdTipoProducto,
                p.IdMarca,
                p.NombreProducto,
                p.Descripción,
                p.Precio,
                p.Stock,
                p.Estado,
                p.Codigo,
                p.FechaIngreso,
                TieneImagen = p.Imagen != null,
                Marca = p.IdMarcaNavigation,
                Tipo = p.IdTipoProductoNavigation,
                Retornable = p.ProProductosRetornables
            })
            .ToListAsync();

        return list.Select(x => new ProProductos
        {
            IdProducto = x.IdProducto,
            IdTipoProducto = x.IdTipoProducto,
            IdMarca = x.IdMarca,
            NombreProducto = x.NombreProducto,
            Descripción = x.Descripción,
            Precio = x.Precio,
            Stock = x.Stock,
            Estado = x.Estado,
            Codigo = x.Codigo,
            FechaIngreso = x.FechaIngreso,
            Imagen = x.TieneImagen ? new byte[1] : null,
            IdMarcaNavigation = x.Marca,
            IdTipoProductoNavigation = x.Tipo,
            ProProductosRetornables = x.Retornable
        });
    }

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
    {
        var list = await context.ProProductos
            .AsNoTracking()
            .OrderByDescending(p => p.FechaIngreso)
            .Take(top)
            .Select(p => new
            {
                p.IdProducto,
                p.IdTipoProducto,
                p.IdMarca,
                p.NombreProducto,
                p.Descripción,
                p.Precio,
                p.Stock,
                p.Estado,
                p.Codigo,
                p.FechaIngreso,
                TieneImagen = p.Imagen != null,
                Marca = p.IdMarcaNavigation
            })
            .ToListAsync();

        return list.Select(x => new ProProductos
        {
            IdProducto = x.IdProducto,
            IdTipoProducto = x.IdTipoProducto,
            IdMarca = x.IdMarca,
            NombreProducto = x.NombreProducto,
            Descripción = x.Descripción,
            Precio = x.Precio,
            Stock = x.Stock,
            Estado = x.Estado,
            Codigo = x.Codigo,
            FechaIngreso = x.FechaIngreso,
            Imagen = x.TieneImagen ? new byte[1] : null,
            IdMarcaNavigation = x.Marca
        });
    }

    // ── Retornables ────────────────────────────────────────────────────────
    public async Task<IEnumerable<ProProductosRetornables>> ObtenerRetornablesAsync()
    {
        var list = await context.ProProductosRetornables
            .AsNoTracking()
            .OrderBy(r => r.IdProductoNavigation.NombreProducto)
            .Select(r => new
            {
                r.IdProducto,
                r.ValorEnvase,
                r.SoloEfectivo,
                // ProProductos columns:
                ProdId = r.IdProductoNavigation.IdProducto,
                ProdNombre = r.IdProductoNavigation.NombreProducto,
                ProdDesc = r.IdProductoNavigation.Descripción,
                ProdPrecio = r.IdProductoNavigation.Precio,
                ProdStock = r.IdProductoNavigation.Stock,
                ProdEstado = r.IdProductoNavigation.Estado,
                ProdCodigo = r.IdProductoNavigation.Codigo,
                ProdFecha = r.IdProductoNavigation.FechaIngreso,
                ProdTipo = r.IdProductoNavigation.IdTipoProducto,
                ProdMarca = r.IdProductoNavigation.IdMarca,
                TieneImagen = r.IdProductoNavigation.Imagen != null,
                Marca = r.IdProductoNavigation.IdMarcaNavigation,
                Tipo = r.IdProductoNavigation.IdTipoProductoNavigation
            })
            .ToListAsync();

        return list.Select(x => {
            var prod = new ProProductos
            {
                IdProducto = x.ProdId,
                NombreProducto = x.ProdNombre,
                Descripción = x.ProdDesc,
                Precio = x.ProdPrecio,
                Stock = x.ProdStock,
                Estado = x.ProdEstado,
                Codigo = x.ProdCodigo,
                FechaIngreso = x.ProdFecha,
                IdTipoProducto = x.ProdTipo,
                IdMarca = x.ProdMarca,
                Imagen = x.TieneImagen ? new byte[1] : null,
                IdMarcaNavigation = x.Marca,
                IdTipoProductoNavigation = x.Tipo
            };
            return new ProProductosRetornables
            {
                IdProducto = x.IdProducto,
                ValorEnvase = x.ValorEnvase,
                SoloEfectivo = x.SoloEfectivo,
                IdProductoNavigation = prod
            };
        });
    }

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

    public async Task<IEnumerable<ProProductos>> BuscarPorCodigoAsync(string codigo)
        => await context.ProProductos
            .AsNoTracking()
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .Include(p => p.ProProductosRetornables)
            .Where(p => p.Codigo != null && p.Codigo == codigo)
            .OrderBy(p => p.NombreProducto)
            .ToListAsync();

    public async Task<ProMarcas> CrearMarcaAsync(ProMarcas marca)
    {
        marca.Estado = true;
        context.ProMarcas.Add(marca);
        await context.SaveChangesAsync();
        return marca;
    }

    public async Task<ProTiposProductos> CrearTipoProductoAsync(ProTiposProductos tipo)
    {
        context.ProTiposProductos.Add(tipo);
        await context.SaveChangesAsync();
        return tipo;
    }
}
