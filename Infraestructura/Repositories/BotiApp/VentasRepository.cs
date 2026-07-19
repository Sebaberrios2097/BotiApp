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
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdOfertaProductoNavigation)
            .Include(b => b.VenMetodosPagoBoleta).ThenInclude(m => m.IdMetodoPagoNavigation);

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

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasPendientesAsync(int top = 20)
        => await BoletasConIncludes()
            .AsNoTracking()
            .Where(b => b.IdEstadoBoleta == 1)
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

    public async Task<VenBoletas?> ObtenerBoletaPorCorrelativoDiarioAsync(int correlativo, DateTime fecha)
    {
        var inicioPeriodo = fecha.Hour >= 8
            ? fecha.Date.AddHours(8)
            : fecha.Date.AddDays(-1).AddHours(8);
        var finPeriodo = inicioPeriodo.AddDays(1);

        return await BoletasConIncludes()
            .FirstOrDefaultAsync(b => b.CorrelativoDiario == correlativo
                                      && b.FechaEmision >= inicioPeriodo
                                      && b.FechaEmision < finPeriodo);
    }

    public async Task<VenBoletas> CrearBoletaAsync(VenBoletas boleta, IEnumerable<VenBoletaDetalle> detalles)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var fecha = boleta.FechaEmision ?? DateTime.Now;
        var inicioPeriodo = fecha.Hour >= 8
            ? fecha.Date.AddHours(8)
            : fecha.Date.AddDays(-1).AddHours(8);
        var finPeriodo = inicioPeriodo.AddDays(1);

        var count = await context.VenBoletas
            .CountAsync(b => b.FechaEmision >= inicioPeriodo && b.FechaEmision < finPeriodo);

        boleta.CorrelativoDiario = count + 1;

        context.VenBoletas.Add(boleta);
        await context.SaveChangesAsync();

        foreach (var item in detalles)
        {
            item.IdBoleta = boleta.IdBoleta;
            context.VenBoletaDetalle.Add(item);

            if (item.IdProducto != 0 && item.PrecioNormal > 0)
            {
                var producto = await context.ProProductos.FindAsync(item.IdProducto);
                if (producto is not null)
                    producto.Stock -= item.Cantidad;
            }
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return boleta;
    }

    public async Task<VenBoletas?> ModificarBoletaDetalleAsync(int idBoleta, IEnumerable<VenBoletaDetalle> nuevosDetalles)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        // Acepta estado Pendiente (1) o Fiado (4)
        var boleta = await context.VenBoletas
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta && (b.IdEstadoBoleta == 1 || b.IdEstadoBoleta == 4));

        if (boleta == null) return null;

        // Restaurar stock de ítems anteriores
        foreach (var old in boleta.VenBoletaDetalle)
        {
            if (old.IdProducto != 0 && old.PrecioNormal > 0)
            {
                var prod = await context.ProProductos.FindAsync(old.IdProducto);
                if (prod != null) prod.Stock += old.Cantidad;
            }
        }

        context.VenBoletaDetalle.RemoveRange(boleta.VenBoletaDetalle);
        await context.SaveChangesAsync();

        // Insertar nuevos ítems y descontar stock
        var lista = nuevosDetalles.ToList();
        foreach (var item in lista)
        {
            item.IdBoleta = idBoleta;
            context.VenBoletaDetalle.Add(item);
            if (item.IdProducto != 0 && item.PrecioNormal > 0)
            {
                var prod = await context.ProProductos.FindAsync(item.IdProducto);
                if (prod != null) prod.Stock -= item.Cantidad;
            }
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
        boleta.IdCajero = idCajero;
        boleta.FechaPago = DateTime.Now;

        foreach (var m in metodos)
        {
            m.IdBoleta = idBoleta;
            context.VenMetodosPagoBoleta.Add(m);
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return await ObtenerBoletaParaCajaAsync(idBoleta);
    }

    public async Task<VenBoletas?> DejarFiadoAsync(int idBoleta, int idClienteFiado, int idCajero)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var boleta = await context.VenBoletas
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta && b.IdEstadoBoleta == 1);

        if (boleta == null) return null;

        boleta.IdEstadoBoleta  = 4; // Fiado
        boleta.IdClienteFiado  = idClienteFiado;
        boleta.IdCajero        = idCajero;
        // FechaPago queda NULL hasta cobro real

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return await ObtenerBoletaParaCajaAsync(idBoleta);
    }

    public async Task<bool> AnularBoletaAsync(int idBoleta, int idUsuario)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        // Acepta estado Pendiente (1) o Fiado (4)
        var boleta = await context.VenBoletas
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta && (b.IdEstadoBoleta == 1 || b.IdEstadoBoleta == 4));

        if (boleta == null) return false;

        // Restaurar stock
        foreach (var d in boleta.VenBoletaDetalle)
        {
            if (d.IdProducto != 0 && d.PrecioNormal > 0)
            {
                var prod = await context.ProProductos.FindAsync(d.IdProducto);
                if (prod != null) prod.Stock += d.Cantidad;
            }
        }

        boleta.IdEstadoBoleta = 2; // Anulada
        boleta.IdCajero = idUsuario;
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return true;
    }

    // ── Catálogo ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ProProductos>> ObtenerProductosDisponiblesAsync()
    {
        var list = await context.ProProductos
            .AsNoTracking()
            .Where(p => p.Estado && p.IdProducto != 0)
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
            .Where(p => p.Estado
                     && p.FechaInicio <= hoy
                     && (p.FechaFin == null || p.FechaFin >= hoy)
                     && p.ProPromocionDetalle.Any()
                     && p.ProPromocionDetalle.All(d => d.IdProductoNavigation.Estado))
            .Include(p => p.ProPromocionGrupo)
            .Include(p => p.ProPromocionDetalle)
                .ThenInclude(d => d.IdProductoNavigation)
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
        var candidatos = await context.ProProductos
            .AsNoTracking()
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .Where(p => p.Estado)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(q))
            candidatos = candidatos.Take(30).ToList();
        else
        {
            var filtroNorm = NormalizarTexto(q);
            candidatos = candidatos
                .Where(p =>
                    NormalizarTexto(p.NombreProducto).Contains(filtroNorm) ||
                    (p.Codigo != null && NormalizarTexto(p.Codigo).Contains(filtroNorm)))
                .OrderBy(p => p.NombreProducto)
                .Take(30)
                .ToList();
        }

        return candidatos.Select(p => new ProProductos
        {
            IdProducto = p.IdProducto,
            IdTipoProducto = p.IdTipoProducto,
            IdMarca = p.IdMarca,
            NombreProducto = p.NombreProducto,
            Descripción = p.Descripción,
            Precio = p.Precio,
            Stock = p.Stock,
            Estado = p.Estado,
            Codigo = p.Codigo,
            FechaIngreso = p.FechaIngreso,
            Imagen = p.Imagen != null ? new byte[1] : null,
            IdMarcaNavigation = p.IdMarcaNavigation,
            IdTipoProductoNavigation = p.IdTipoProductoNavigation
        });
    }

    /// <summary>
    /// Normaliza un texto para búsquedas: minúsculas, sin acentos, sin separadores
    /// (espacios, guiones, guiones bajos, puntos, comas, etc.). De este modo,
    /// "coca cola", "coca-cola", "coca_cola" y "cocacola" son equivalentes.
    /// </summary>
    private static string NormalizarTexto(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return string.Empty;

        var sinAcentos = texto.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(sinAcentos.Length);
        foreach (var c in sinAcentos)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) ==
                System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasDelMesAsync(int anio, int mes)
        => await BoletasConIncludes()
            .AsNoTracking()
            .Where(b => b.FechaEmision.HasValue
                     && b.FechaEmision.Value.Year == anio
                     && b.FechaEmision.Value.Month == mes)
            .OrderByDescending(b => b.FechaEmision)
            .ToListAsync();

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasVendedorDelMesAsync(int idVendedor, int anio, int mes)
        => await BoletasConIncludes()
            .AsNoTracking()
            .Where(b => b.IdVendedor == idVendedor
                     && b.FechaEmision.HasValue
                     && b.FechaEmision.Value.Year == anio
                     && b.FechaEmision.Value.Month == mes)
            .OrderByDescending(b => b.FechaEmision)
            .ToListAsync();

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasCajeroDelMesAsync(int idCajero, int anio, int mes)
        => await BoletasConIncludes()
            .AsNoTracking()
            .Where(b => b.IdCajero == idCajero
                     && b.FechaEmision.HasValue
                     && b.FechaEmision.Value.Year == anio
                     && b.FechaEmision.Value.Month == mes)
            .OrderByDescending(b => b.FechaEmision)
            .ToListAsync();

    public async Task<IEnumerable<(int Anio, int Mes)>> ObtenerPeriodosConMovimientoAsync()
    {
        var raw = await context.VenBoletas
            .Where(b => b.FechaEmision.HasValue)
            .Select(b => new { Year = b.FechaEmision!.Value.Year, Month = b.FechaEmision.Value.Month })
            .Distinct()
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .ToListAsync();
        return raw.Select(x => (x.Year, x.Month));
    }
}
