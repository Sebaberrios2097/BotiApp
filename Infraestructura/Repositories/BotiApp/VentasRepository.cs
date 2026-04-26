using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class VentasRepository(BotiAppContext context) : IVentasRepository
{
    // ── Boletas ───────────────────────────────────────────────────────────────

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasAsync()
        => await context.VenBoletas
            .AsNoTracking()
            .Include(b => b.IdEstadoBoletaNavigation)
            .Include(b => b.IdUsuarioNavigation).ThenInclude(u => u.IdEmpleadoNavigation)
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdProductoNavigation)
            .OrderByDescending(b => b.FechaEmision)
            .ToListAsync();

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasPorUsuarioAsync(int idUsuario, int top = 15)
        => await context.VenBoletas
            .AsNoTracking()
            .Where(b => b.IdUsuario == idUsuario)
            .Include(b => b.IdEstadoBoletaNavigation)
            .Include(b => b.IdUsuarioNavigation).ThenInclude(u => u.IdEmpleadoNavigation)
            .Include(b => b.VenBoletaDetalle)
            .OrderByDescending(b => b.FechaEmision)
            .Take(top)
            .ToListAsync();

    public async Task<IEnumerable<VenBoletas>> ObtenerUltimasBoletasSistemaAsync(int top = 15)
        => await context.VenBoletas
            .AsNoTracking()
            .Include(b => b.IdEstadoBoletaNavigation)
            .Include(b => b.IdUsuarioNavigation).ThenInclude(u => u.IdEmpleadoNavigation)
            .Include(b => b.VenBoletaDetalle)
            .OrderByDescending(b => b.FechaEmision)
            .Take(top)
            .ToListAsync();

    public async Task<VenBoletas?> ObtenerPorIdAsync(int id)
        => await context.VenBoletas
            .AsNoTracking()
            .Include(b => b.IdEstadoBoletaNavigation)
            .Include(b => b.IdUsuarioNavigation).ThenInclude(u => u.IdEmpleadoNavigation)
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdProductoNavigation)
            .FirstOrDefaultAsync(b => b.IdBoleta == id);

    public async Task<VenBoletas> CrearBoletaAsync(VenBoletas boleta, IEnumerable<VenBoletaDetalle> detalles)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        context.VenBoletas.Add(boleta);
        await context.SaveChangesAsync();

        foreach (var item in detalles)
        {
            item.IdBoleta = boleta.IdBoleta;
            context.VenBoletaDetalle.Add(item);

            var producto = await context.ProProductos.FindAsync(item.IdProducto);
            if (producto is not null)
                producto.Stock -= item.Cantidad;
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return boleta;
    }

    // ── Catálogo ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ProProductos>> ObtenerProductosDisponiblesAsync()
        => await context.ProProductos
            .AsNoTracking()
            .Where(p => p.Estado && p.Stock > 0)
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .OrderBy(p => p.NombreProducto)
            .ToListAsync();

    public async Task<IEnumerable<ProTiposProductos>> ObtenerTiposAsync()
        => await context.ProTiposProductos
            .AsNoTracking()
            .OrderBy(t => t.NombreTipoProducto)
            .ToListAsync();

    public async Task<IEnumerable<ProMarcas>> ObtenerMarcasAsync()
        => await context.ProMarcas
            .AsNoTracking()
            .Where(m => m.Estado)
            .OrderBy(m => m.NombreMarca)
            .ToListAsync();

    // ── Promociones activas ────────────────────────────────────────────────────

    public async Task<IEnumerable<ProPromocion>> ObtenerPromocionesActivasAsync()
    {
        var hoy = DateTime.Today;
        return await context.ProPromocion
            .AsNoTracking()
            .Where(p => p.Estado && p.FechaInicio <= hoy && (p.FechaFin == null || p.FechaFin >= hoy))
            .Include(p => p.ProPromocionGrupo)
            .Include(p => p.ProPromocionDetalle)
            .ToListAsync();
    }

    // ── Ofertas activas ────────────────────────────────────────────────────────

    public async Task<IEnumerable<ProOfertaProducto>> ObtenerOfertasActivasAsync()
    {
        var hoy = DateTime.Today;
        return await context.ProOfertaProducto
            .AsNoTracking()
            .Where(o => o.Estado
                     && o.FechaInicioOferta <= hoy
                     && (o.FechaTerminoOferta == null || o.FechaTerminoOferta >= hoy))
            .ToListAsync();
    }

    // ── Métodos de pago ───────────────────────────────────────────────────────

    public async Task<IEnumerable<VenMetodosPago>> ObtenerMetodosPagoAsync()
        => await context.VenMetodosPago
            .AsNoTracking()
            .OrderBy(m => m.NombreMetodoPago)
            .ToListAsync();

    // ── Buscador (mantenido para otros usos) ──────────────────────────────────

    public async Task<IEnumerable<ProProductos>> BuscarProductosAsync(string q)
    {
        var query = context.ProProductos
            .AsNoTracking()
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .Where(p => p.Estado && p.Stock > 0);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p =>
                p.NombreProducto.Contains(q) ||
                (p.Descripción != null && p.Descripción.Contains(q)) ||
                (p.IdMarcaNavigation != null && p.IdMarcaNavigation.NombreMarca.Contains(q)));

        return await query.OrderBy(p => p.NombreProducto).Take(30).ToListAsync();
    }
}