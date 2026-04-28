using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class VentasRepository(BotiAppContext context) : IVentasRepository
{
    // ── Boletas ───────────────────────────────────────────────────────────────

    private IQueryable<VenBoletas> BoletasConIncludes()
        => context.VenBoletas
            .Include(b => b.IdEstadoBoletaNavigation)
            .Include(b => b.IdVendedorNavigation).ThenInclude(u => u.IdEmpleadoNavigation)
            .Include(b => b.IdCajeroNavigation).ThenInclude(u => u!.IdEmpleadoNavigation)
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdProductoNavigation)
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdPromocionNavigation)
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdOfertaProductoNavigation);

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasAsync()
        => await BoletasConIncludes()
            .AsNoTracking()
            .OrderByDescending(b => b.FechaEmision)
            .ToListAsync();

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasPorVendedorAsync(int idVendedor, int top = 100)
        => await BoletasConIncludes()
            .AsNoTracking()
            .Where(b => b.IdVendedor == idVendedor)
            .OrderByDescending(b => b.FechaEmision)
            .Take(top)
            .ToListAsync();

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasPorCajeroAsync(int idCajero, int top = 100)
        => await BoletasConIncludes()
            .AsNoTracking()
            .Where(b => b.IdCajero == idCajero)
            .OrderByDescending(b => b.FechaEmision)
            .Take(top)
            .ToListAsync();

    public async Task<IEnumerable<VenBoletas>> ObtenerUltimasBoletasSistemaAsync(int top = 15)
        => await BoletasConIncludes()
            .AsNoTracking()
            .OrderByDescending(b => b.FechaEmision)
            .Take(top)
            .ToListAsync();

    public async Task<VenBoletas?> ObtenerPorIdAsync(int id)
        => await BoletasConIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.IdBoleta == id);

    public async Task<VenBoletas?> ObtenerBoletaParaCajaAsync(int id)
        => await BoletasConIncludes()
            .AsNoTracking()
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

    public async Task<VenBoletas?> ModificarBoletaDetalleAsync(int idBoleta, IEnumerable<VenBoletaDetalle> nuevosDetalles)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var boleta = await context.VenBoletas
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta && b.IdEstadoBoleta == 1);

        if (boleta == null) return null;

        // Restaurar stock de ítems anteriores
        foreach (var old in boleta.VenBoletaDetalle)
        {
            var prod = await context.ProProductos.FindAsync(old.IdProducto);
            if (prod != null) prod.Stock += old.Cantidad;
        }

        context.VenBoletaDetalle.RemoveRange(boleta.VenBoletaDetalle);
        await context.SaveChangesAsync();

        // Insertar nuevos ítems y descontar stock
        var lista = nuevosDetalles.ToList();
        foreach (var item in lista)
        {
            item.IdBoleta = idBoleta;
            context.VenBoletaDetalle.Add(item);
            var prod = await context.ProProductos.FindAsync(item.IdProducto);
            if (prod != null) prod.Stock -= item.Cantidad;
        }

        boleta.MontoTotal = lista.Sum(d => d.Subtotal);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return await ObtenerBoletaParaCajaAsync(idBoleta);
    }

    public async Task<VenBoletas?> CobrarBoletaAsync(int idBoleta, int idCajero, IEnumerable<VenMetodosPagoBoleta> metodos)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var boleta = await context.VenBoletas
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta && b.IdEstadoBoleta == 1);

        if (boleta == null) return null;

        boleta.IdEstadoBoleta = 3; // Pagada
        boleta.IdCajero       = idCajero;
        boleta.FechaPago      = DateTime.Now;

        foreach (var m in metodos)
        {
            m.IdBoleta = idBoleta;
            context.VenMetodosPagoBoleta.Add(m);
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return await ObtenerBoletaParaCajaAsync(idBoleta);
    }

    public async Task<bool> AnularBoletaAsync(int idBoleta)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var boleta = await context.VenBoletas
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta && b.IdEstadoBoleta == 1);

        if (boleta == null) return false;

        // Restaurar stock
        foreach (var d in boleta.VenBoletaDetalle)
        {
            var prod = await context.ProProductos.FindAsync(d.IdProducto);
            if (prod != null) prod.Stock += d.Cantidad;
        }

        boleta.IdEstadoBoleta = 2; // Anulada
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return true;
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
