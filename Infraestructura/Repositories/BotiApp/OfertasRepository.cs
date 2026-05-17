using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class OfertasRepository(BotiAppContext context) : IOfertasRepository
{
    public async Task<ProProductos?> ObtenerProductoAsync(int idProducto)
        => await context.ProProductos
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProducto == idProducto);

    public async Task<ProOfertaProducto?> ObtenerActivaPorProductoAsync(int idProducto)
    {
        var hoy = DateTime.Today;
        return await context.ProOfertaProducto
            .AsNoTracking()
            .Where(o => o.IdProducto == idProducto
                     && o.Estado
                     && o.FechaInicioOferta.Date <= hoy
                     && (o.FechaTerminoOferta == null || o.FechaTerminoOferta.Value.Date >= hoy))
            .OrderByDescending(o => o.FechaInicioOferta)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ProOfertaProducto>> ObtenerHistorialPorProductoAsync(int idProducto)
        => await context.ProOfertaProducto
            .AsNoTracking()
            .Where(o => o.IdProducto == idProducto)
            .OrderByDescending(o => o.FechaInicioOferta)
            .ToListAsync();

    public async Task<IEnumerable<ProPromocion>> ObtenerPromosActivasPorProductoAsync(int idProducto)
    {
        var hoy = DateTime.Today;
        return await context.ProPromocionDetalle
            .Include(d => d.IdPromocionNavigation)
            .Where(d => d.IdProducto == idProducto
                     && d.IdPromocionNavigation.Estado
                     && d.IdPromocionNavigation.FechaInicio.Date <= hoy
                     && (d.IdPromocionNavigation.FechaFin == null || d.IdPromocionNavigation.FechaFin.Value.Date >= hoy))
            .Select(d => d.IdPromocionNavigation)
            .Distinct()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ProOfertaProducto> CrearAsync(ProOfertaProducto oferta)
    {
        context.ProOfertaProducto.Add(oferta);
        await context.SaveChangesAsync();
        return oferta;
    }

    public async Task<ProOfertaProducto?> ObtenerPorIdAsync(int id)
        => await context.ProOfertaProducto.FindAsync(id);

    public async Task<bool> DesactivarAsync(int idOferta)
    {
        var oferta = await context.ProOfertaProducto.FindAsync(idOferta);
        if (oferta is null) return false;
        oferta.Estado = false;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivarAsync(int idOferta)
    {
        var oferta = await context.ProOfertaProducto.FindAsync(idOferta);
        if (oferta is null) return false;

        // Solo activar si el producto no tiene ya una oferta activa
        var activa = await ObtenerActivaPorProductoAsync(oferta.IdProducto);
        if (activa is not null) return false;

        oferta.Estado = true;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActualizarAsync(int idOferta, int precioOferta, DateTime fechaInicio, DateTime? fechaTermino)
    {
        var oferta = await context.ProOfertaProducto.FindAsync(idOferta);
        if (oferta is null) return false;
        oferta.PrecioOferta       = precioOferta;
        oferta.FechaInicioOferta  = fechaInicio;
        oferta.FechaTerminoOferta = fechaTermino;
        await context.SaveChangesAsync();
        return true;
    }
}
