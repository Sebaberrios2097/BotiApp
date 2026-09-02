using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class VentasRepository(BotiAppContext context, IProductosRepository productosRepository) : IVentasRepository
{
    // ── Stock pack-aware ──────────────────────────────────────────────────────
    // La lógica de stock pack-aware vive en ProductosRepository.AplicarDeltaStockAsync.


    // ── Boletas ───────────────────────────────────────────────────────────────

    // AsSplitQuery evita el producto cartesiano entre VenBoletaDetalle y
    // VenMetodosPagoBoleta (dos colecciones hijas incluidas a la vez), que sin
    // esto multiplica las filas devueltas por SQL para cada boleta. Además,
    // permite paginar (Skip/Take) directamente sobre la consulta con includes:
    // EF Core pagina la consulta raíz en una sola pasada y luego trae el detalle
    // solo de esas filas, sin necesidad de buscar IDs de página aparte.
    private static IQueryable<VenBoletas> ConIncludes(IQueryable<VenBoletas> q)
        => q
            .AsSplitQuery()
            .Include(b => b.IdEstadoBoletaNavigation)
            .Include(b => b.IdVendedorNavigation).ThenInclude(u => u.IdEmpleadoNavigation)
            .Include(b => b.IdCajeroNavigation).ThenInclude(u => u!.IdEmpleadoNavigation)
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdProductoNavigation)
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdPromocionNavigation)
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdOfertaProductoNavigation)
            .Include(b => b.VenMetodosPagoBoleta).ThenInclude(m => m.IdMetodoPagoNavigation);

    private IQueryable<VenBoletas> BoletasConIncludes() => ConIncludes(context.VenBoletas);

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

    // Las ventas postergadas (5) todavía no son ventas emitidas: no deben poder
    // abrirse desde Caja, ni siquiera buscándolas por número.
    public async Task<VenBoletas?> ObtenerBoletaParaCajaAsync(int id)
        => await BoletasConIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.IdBoleta == id && b.IdEstadoBoleta != EstadoSinFinalizar);

    public async Task<VenBoletas?> ObtenerBoletaPorCorrelativoDiarioAsync(int correlativo, DateTime fecha)
    {
        var inicioPeriodo = fecha.Hour >= 8
            ? fecha.Date.AddHours(8)
            : fecha.Date.AddDays(-1).AddHours(8);
        var finPeriodo = inicioPeriodo.AddDays(1);

        // Las postergadas ya tienen su correlativo reservado, pero todavía no son
        // ventas emitidas: se excluyen para que Caja no pueda abrirlas por número.
        return await BoletasConIncludes()
            .FirstOrDefaultAsync(b => b.CorrelativoDiario == correlativo
                                      && b.IdEstadoBoleta != EstadoSinFinalizar
                                      && b.FechaEmision >= inicioPeriodo
                                      && b.FechaEmision < finPeriodo);
    }

    // Siguiente correlativo de la jornada (que corre de 8:00 a 8:00). Se toma el
    // máximo y no la cantidad de boletas para no reutilizar números liberados al
    // descartar una venta postergada.
    private async Task<int> SiguienteCorrelativoDiarioAsync(DateTime fecha)
    {
        var inicioPeriodo = fecha.Hour >= 8
            ? fecha.Date.AddHours(8)
            : fecha.Date.AddDays(-1).AddHours(8);
        var finPeriodo = inicioPeriodo.AddDays(1);

        var ultimoCorrelativo = await context.VenBoletas
            .Where(b => b.FechaEmision >= inicioPeriodo && b.FechaEmision < finPeriodo
                        && b.CorrelativoDiario != null)
            .MaxAsync(b => (int?)b.CorrelativoDiario) ?? 0;

        return ultimoCorrelativo + 1;
    }

    public async Task<VenBoletas> CrearBoletaAsync(VenBoletas boleta, IEnumerable<VenBoletaDetalle> detalles)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        boleta.CorrelativoDiario = await SiguienteCorrelativoDiarioAsync(boleta.FechaEmision ?? DateTime.Now);

        context.VenBoletas.Add(boleta);
        await context.SaveChangesAsync();

        foreach (var item in detalles)
        {
            item.IdBoleta = boleta.IdBoleta;
            context.VenBoletaDetalle.Add(item);

            if (item.IdProducto != 0 && item.PrecioNormal > 0)
            {
                await productosRepository.AplicarDeltaStockAsync(item.IdProducto, -item.Cantidad);
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
                await productosRepository.AplicarDeltaStockAsync(old.IdProducto, +old.Cantidad);
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
                await productosRepository.AplicarDeltaStockAsync(item.IdProducto, -item.Cantidad);
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

    public async Task<bool> AnularBoletaAsync(int idBoleta, int idUsuario, string? nota = null)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        // Acepta estado Pendiente (1), Pagada (3) o Fiado (4)
        var boleta = await context.VenBoletas
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta
                                      && (b.IdEstadoBoleta == 1
                                       || b.IdEstadoBoleta == 3
                                       || b.IdEstadoBoleta == 4));

        if (boleta == null) return false;

        // Restaurar stock
        foreach (var d in boleta.VenBoletaDetalle)
        {
            if (d.IdProducto != 0 && d.PrecioNormal > 0)
            {
                await productosRepository.AplicarDeltaStockAsync(d.IdProducto, +d.Cantidad);
            }
        }

        boleta.IdEstadoBoleta = 2; // Anulada

        // Se conserva al cajero que cobró o fió la boleta: solo se registra al usuario
        // que anula cuando la boleta seguía pendiente y no tenía cajero asignado.
        boleta.IdCajero ??= idUsuario;

        // Los métodos de pago se mantienen como respaldo del cobro original; los
        // informes filtran por estado, así que una boleta anulada ya no los suma.
        if (!string.IsNullOrWhiteSpace(nota))
        {
            boleta.Observaciones = string.IsNullOrWhiteSpace(boleta.Observaciones)
                ? nota
                : $"{boleta.Observaciones}{Environment.NewLine}{nota}";
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return true;
    }

    // ── Ventas postergadas (estado 5 «Sin Finalizar») ─────────────────────────
    private const int EstadoSinFinalizar = 5;

    public async Task<VenBoletas> PostergarVentaAsync(VenBoletas boleta, IEnumerable<VenBoletaDetalle> detalles)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        boleta.IdEstadoBoleta = EstadoSinFinalizar;

        // El correlativo se reserva desde ya y la boleta lo conserva al emitirse
        // (EmitirVentaPostergadaAsync), así el número que ve el vendedor en el panel
        // es el mismo que saldrá impreso en el ticket. Descartarla libera el número
        // sin reutilizarlo, dejando un salto en la numeración del día.
        boleta.CorrelativoDiario = await SiguienteCorrelativoDiarioAsync(boleta.FechaEmision ?? DateTime.Now);

        context.VenBoletas.Add(boleta);
        await context.SaveChangesAsync();

        // No se toca el stock: la venta no se emitió, así que los productos siguen
        // disponibles para otro cliente mientras este se decide.
        foreach (var item in detalles)
        {
            item.IdBoleta = boleta.IdBoleta;
            context.VenBoletaDetalle.Add(item);
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return boleta;
    }

    // Reemplaza el detalle de una venta ya postergada conservando su identidad, para
    // que reeditarla y volver a postergarla no genere una boleta nueva cada vez.
    // Igual que al postergar, no se mueve stock: la venta sigue sin emitirse.
    public async Task<VenBoletas?> ActualizarVentaPostergadaAsync(
        int idBoleta, int montoTotal, IEnumerable<VenBoletaDetalle> detalles)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var boleta = await context.VenBoletas
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta
                                      && b.IdEstadoBoleta == EstadoSinFinalizar);
        if (boleta is null) return null;

        context.VenBoletaDetalle.RemoveRange(boleta.VenBoletaDetalle);
        await context.SaveChangesAsync();

        var lista = detalles.ToList();
        foreach (var item in lista)
        {
            item.IdBoleta = idBoleta;
            context.VenBoletaDetalle.Add(item);
        }

        boleta.MontoTotal = montoTotal > 0 ? montoTotal : lista.Sum(d => d.Subtotal);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return boleta;
    }

    // Emite una venta postergada reutilizando su boleta: conserva el IdBoleta y el
    // correlativo que ya se le había reservado, así el número del panel es el que
    // sale en el ticket. La fecha de emisión NO se actualiza: el correlativo
    // pertenece a la jornada en que se postergó y moverla podría chocar con la
    // numeración de la jornada siguiente. Aquí sí se descuenta stock, porque es
    // el momento en que la venta se concreta.
    public async Task<VenBoletas?> EmitirVentaPostergadaAsync(
        int idBoleta, int idVendedor, IEnumerable<VenBoletaDetalle> detalles)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var boleta = await context.VenBoletas
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta
                                      && b.IdEstadoBoleta == EstadoSinFinalizar);
        if (boleta is null) return null;

        context.VenBoletaDetalle.RemoveRange(boleta.VenBoletaDetalle);
        await context.SaveChangesAsync();

        var lista = detalles.ToList();
        foreach (var item in lista)
        {
            item.IdBoleta = idBoleta;
            context.VenBoletaDetalle.Add(item);

            if (item.IdProducto != 0 && item.PrecioNormal > 0)
            {
                await productosRepository.AplicarDeltaStockAsync(item.IdProducto, -item.Cantidad);
            }
        }

        boleta.IdEstadoBoleta = 1;
        boleta.IdVendedor     = idVendedor;
        boleta.MontoTotal     = lista.Sum(d => d.Subtotal);

        // Si se postergó sin correlativo (ventas anteriores a esta lógica), se le
        // asigna uno al emitirla para no dejar el ticket sin número.
        boleta.CorrelativoDiario ??= await SiguienteCorrelativoDiarioAsync(boleta.FechaEmision ?? DateTime.Now);

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return boleta;
    }

    public async Task<IEnumerable<VenBoletas>> ObtenerVentasPostergadasAsync(int top = 30)
        => await BoletasConIncludes()
            .AsNoTracking()
            .Where(b => b.IdEstadoBoleta == EstadoSinFinalizar)
            .OrderByDescending(b => b.FechaEmision)
            .Take(top)
            .ToListAsync();

    // Solo lectura: la venta postergada sigue guardada después de cargarla en el
    // carrito, para que el vendedor pueda alternar entre varias sin perderlas. Se
    // elimina recién al emitirla o al vaciar el carrito (DescartarVentaPostergadaAsync).
    public async Task<VenBoletas?> RecuperarVentaPostergadaAsync(int idBoleta)
        => await context.VenBoletas
            .AsNoTracking()
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta
                                      && b.IdEstadoBoleta == EstadoSinFinalizar);

    // Devuelve el número visible de la venta descartada, o null si ya no existía,
    // para poder nombrarla en el aviso al vendedor.
    public async Task<int?> DescartarVentaPostergadaAsync(int idBoleta)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var boleta = await context.VenBoletas
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta
                                      && b.IdEstadoBoleta == EstadoSinFinalizar);
        if (boleta is null) return null;

        var numero = boleta.CorrelativoDiario ?? boleta.IdBoleta;

        context.VenBoletaDetalle.RemoveRange(boleta.VenBoletaDetalle);
        context.VenBoletas.Remove(boleta);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return numero;
    }

    // ── Catálogo ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ProProductos>> ObtenerProductosDisponiblesAsync()
    {
        var ventasPorProducto = await context.VenBoletaDetalle
            .AsNoTracking()
            .Where(d => d.IdBoletaNavigation.IdEstadoBoleta == 3)
            .GroupBy(d => d.IdProducto)
            .Select(g => new { IdProducto = g.Key, Cantidad = g.Sum(d => d.Cantidad) })
            .ToDictionaryAsync(x => x.IdProducto, x => x.Cantidad);

        var list = await context.ProProductos
            .AsNoTracking()
            .Where(p => p.Estado && p.IdProducto != 0)
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

        return list
            .OrderByDescending(x => ventasPorProducto.GetValueOrDefault(x.IdProducto, 0))
            .ThenBy(x => x.NombreProducto)
            .Select(x => new ProProductos
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
    // El dashboard solo necesita IdProducto/Cantidad/Subtotal de cada línea (no
    // el producto/promoción/oferta completos) y el nombre del método de pago, así
    // que se usa un include más liviano que BoletasConIncludes para no arrastrar
    // el grafo completo de cada boleta del mes (potencialmente miles de filas).
    private IQueryable<VenBoletas> BoletasDashboardIncludes()
        => context.VenBoletas
            .AsSplitQuery()
            .Include(b => b.IdEstadoBoletaNavigation)
            .Include(b => b.IdVendedorNavigation).ThenInclude(u => u.IdEmpleadoNavigation)
            .Include(b => b.IdCajeroNavigation).ThenInclude(u => u!.IdEmpleadoNavigation)
            .Include(b => b.VenBoletaDetalle)
            .Include(b => b.VenMetodosPagoBoleta).ThenInclude(m => m.IdMetodoPagoNavigation);

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasDelMesAsync(int anio, int mes)
        => await BoletasDashboardIncludes()
            .AsNoTracking()
            .Where(b => b.FechaEmision.HasValue
                     && b.FechaEmision.Value.Year == anio
                     && b.FechaEmision.Value.Month == mes)
            .OrderByDescending(b => b.FechaEmision)
            .ToListAsync();

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasVendedorDelMesAsync(int idVendedor, int anio, int mes)
        => await BoletasDashboardIncludes()
            .AsNoTracking()
            .Where(b => b.IdVendedor == idVendedor
                     && b.FechaEmision.HasValue
                     && b.FechaEmision.Value.Year == anio
                     && b.FechaEmision.Value.Month == mes)
            .OrderByDescending(b => b.FechaEmision)
            .ToListAsync();

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasCajeroDelMesAsync(int idCajero, int anio, int mes)
        => await BoletasDashboardIncludes()
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

    // ── Historial (paginado, filtrado en servidor) ──────────────────────────────
    // El historial puede acumular años de boletas: en vez de traer todo el grafo
    // de entidades para filtrar/paginar en memoria, se filtra y pagina a nivel SQL
    // (solo columnas escalares) y recién ahí se cargan con include las boletas de
    // la página actual.
    public async Task<(IReadOnlyList<VenBoletas> Items, int Total)> ObtenerBoletasHistorialAsync(
        int? idVendedorScope, int? idCajeroScope, int? idVendedorFiltro,
        int? estado, int? anio, int? mes, int? dia, string? texto,
        int pagina, int porPagina)
    {
        var q = context.VenBoletas.AsNoTracking().AsQueryable();

        if (idVendedorScope.HasValue) q = q.Where(b => b.IdVendedor == idVendedorScope.Value);
        if (idCajeroScope.HasValue) q = q.Where(b => b.IdCajero == idCajeroScope.Value);
        if (idVendedorFiltro.HasValue && idVendedorFiltro.Value != 0) q = q.Where(b => b.IdVendedor == idVendedorFiltro.Value);
        if (estado.HasValue && estado.Value != 0) q = q.Where(b => b.IdEstadoBoleta == estado.Value);
        if (anio.HasValue) q = q.Where(b => b.FechaEmision.HasValue && b.FechaEmision.Value.Year == anio.Value);
        if (mes.HasValue) q = q.Where(b => b.FechaEmision.HasValue && b.FechaEmision.Value.Month == mes.Value);
        if (dia.HasValue) q = q.Where(b => b.FechaEmision.HasValue && b.FechaEmision.Value.Day == dia.Value);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim();
            var matchFechaDdMm = System.Text.RegularExpressions.Regex.Match(t, @"^(\d{1,2})[/\-](\d{1,2})$");

            if (int.TryParse(t, out var numero))
            {
                q = q.Where(b => b.IdBoleta == numero
                               || b.CorrelativoDiario == numero
                               || (b.IdVendedorNavigation.IdEmpleadoNavigation!.NombresEmpleado + " " + b.IdVendedorNavigation.IdEmpleadoNavigation.Apellido1).Contains(t));
            }
            else if (matchFechaDdMm.Success)
            {
                var d = int.Parse(matchFechaDdMm.Groups[1].Value);
                var m = int.Parse(matchFechaDdMm.Groups[2].Value);
                q = q.Where(b => b.FechaEmision.HasValue && b.FechaEmision.Value.Day == d && b.FechaEmision.Value.Month == m);
            }
            else
            {
                q = q.Where(b => (b.IdVendedorNavigation.IdEmpleadoNavigation!.NombresEmpleado + " " + b.IdVendedorNavigation.IdEmpleadoNavigation.Apellido1).Contains(t));
            }
        }

        var total = await q.CountAsync();
        if (total == 0)
            return (Array.Empty<VenBoletas>(), 0);

        // Paginar directamente sobre la consulta con includes (AsSplitQuery aplica
        // el mismo Skip/Take/OrderBy a las consultas de detalle y métodos de pago),
        // en vez de buscar IDs de página y volver a consultar por separado.
        var items = await ConIncludes(q)
            .OrderByDescending(b => b.IdBoleta)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .ToListAsync();

        return (items, total);
    }

    public async Task<IEnumerable<int>> ObtenerAniosConVentasAsync(int? idVendedorScope, int? idCajeroScope)
    {
        var q = context.VenBoletas.Where(b => b.FechaEmision.HasValue).AsQueryable();
        if (idVendedorScope.HasValue) q = q.Where(b => b.IdVendedor == idVendedorScope.Value);
        if (idCajeroScope.HasValue) q = q.Where(b => b.IdCajero == idCajeroScope.Value);

        return await q
            .Select(b => b.FechaEmision!.Value.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();
    }

    public async Task<IEnumerable<(int IdUsuario, string Nombre)>> ObtenerVendedoresConVentasAsync()
    {
        var raw = await context.VenBoletas
            .Select(b => new
            {
                b.IdVendedor,
                Nombre = b.IdVendedorNavigation.IdEmpleadoNavigation!.NombresEmpleado + " " + b.IdVendedorNavigation.IdEmpleadoNavigation.Apellido1
            })
            .Distinct()
            .OrderBy(x => x.Nombre)
            .ToListAsync();
        return raw.Select(x => (x.IdVendedor, x.Nombre));
    }
}
