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
            .AsNoTracking()
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

    // ── Packs ────────────────────────────────────────────────────────────────
    public async Task<ProProductoPack?> ObtenerPackPorProductoAsync(int idProducto)
        => await context.ProProductoPack
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProductoPackProducto == idProducto);

    public async Task<ProProductoPack> UpsertPackAsync(ProProductoPack pack)
    {
        var existente = await context.ProProductoPack
            .FirstOrDefaultAsync(p => p.IdProductoPackProducto == pack.IdProductoPackProducto);
        if (existente is null)
        {
            pack.FechaCreacion = DateTime.Now;
            pack.Estado = true;
            context.ProProductoPack.Add(pack);
        }
        else
        {
            existente.IdProductoUnidad = pack.IdProductoUnidad;
            existente.CantidadUnidades = pack.CantidadUnidades;
            existente.Estado = pack.Estado;
            context.ProProductoPack.Update(existente);
            pack = existente;
        }

        // Sincronizar stock del producto pack con la unidad base.
        // El stock del pack SIEMPRE equivale a floor(stock_unidad / cantidad).
        if (pack.CantidadUnidades > 0)
        {
            var unidad = await context.ProProductos.FindAsync(pack.IdProductoUnidad);
            var packProd = await context.ProProductos.FindAsync(pack.IdProductoPackProducto);
            if (unidad is not null && packProd is not null)
                packProd.Stock = unidad.Stock / pack.CantidadUnidades;
        }

        await context.SaveChangesAsync();
        return pack;
    }

    public async Task<bool> EliminarPackPorProductoAsync(int idProducto)
    {
        var p = await context.ProProductoPack
            .FirstOrDefaultAsync(x => x.IdProductoPackProducto == idProducto);
        if (p is null) return false;
        context.ProProductoPack.Remove(p);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<HashSet<int>> ObtenerIdsUnidadOcupadosAsync()
    {
        var ids = await context.ProProductoPack
            .AsNoTracking()
            .Select(p => p.IdProductoUnidad)
            .ToListAsync();
        return ids.ToHashSet();
    }

    /// <summary>
    /// Aplica un delta al stock de un producto y mantiene sincronizado el stock del pack
    /// asociado (tanto si el producto es pack como si es unidad de un pack).
    ///
    /// Reglas:
    ///   • Si el producto es un PACK: descuenta/suma `delta` al pack, y propaga
    ///     `delta * CantidadUnidades` a la unidad base. Ambos stocks quedan persistidos.
    ///   • Si el producto es la UNIDAD de un pack: aplica el delta a la unidad y
    ///     recalcula el stock del pack como `floor(StockUnidad / CantidadUnidades)`,
    ///     aplicando el delta resultante al pack.
    ///   • Si no es ni pack ni unidad de pack: solo aplica el delta al producto.
    ///
    /// `delta` positivo = entrada de stock; negativo = salida.
    /// </summary>
    public async Task AplicarDeltaStockAsync(int idProducto, int delta)
    {
        if (delta == 0) return;
        var prod = await context.ProProductos.FindAsync(idProducto);
        if (prod is null) throw new InvalidOperationException($"Producto {idProducto} no encontrado.");

        var packComoPack = await context.ProProductoPack
            .FirstOrDefaultAsync(pk => pk.IdProductoPackProducto == idProducto);
        var packComoUnidad = await context.ProProductoPack
            .FirstOrDefaultAsync(pk => pk.IdProductoUnidad == idProducto);

        // Caso 1: el producto ES un pack
        if (packComoPack is not null)
        {
            prod.Stock += delta;
            var unidad = await context.ProProductos.FindAsync(packComoPack.IdProductoUnidad)
                ?? throw new InvalidOperationException($"Unidad base del pack {idProducto} no encontrada.");
            unidad.Stock += delta * packComoPack.CantidadUnidades;
            return;
        }

        // Caso 2: el producto es la UNIDAD de un pack
        if (packComoUnidad is not null)
        {
            prod.Stock += delta;
            var packProd = await context.ProProductos.FindAsync(packComoUnidad.IdProductoPackProducto)
                ?? throw new InvalidOperationException($"Producto pack {packComoUnidad.IdProductoPackProducto} no encontrado.");
            var stockPackNuevo = packComoUnidad.CantidadUnidades > 0
                ? prod.Stock / packComoUnidad.CantidadUnidades
                : 0;
            var deltaPack = stockPackNuevo - packProd.Stock;
            if (deltaPack != 0) packProd.Stock += deltaPack;
            return;
        }

        // Caso 3: producto independiente
        prod.Stock += delta;
    }

    public async Task<IEnumerable<ProProductos>> BuscarAsync(string filtro)
    {
        filtro = (filtro ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(filtro))
            return Enumerable.Empty<ProProductos>();

        var terminos = filtro
            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLower())
            .Where(t => t.Length > 0)
            .ToArray();

        if (terminos.Length == 0)
            return Enumerable.Empty<ProProductos>();

        var query = context.ProProductos.AsNoTracking().AsQueryable();

        var primerTermino = terminos[0];
        query = query.Where(p =>
            EF.Functions.Like(p.NombreProducto.ToLower(), "%" + primerTermino + "%") ||
            (p.Codigo != null && EF.Functions.Like(p.Codigo.ToLower(), "%" + primerTermino + "%")));

        for (var i = 1; i < terminos.Length; i++)
        {
            var t = terminos[i];
            query = query.Where(p =>
                EF.Functions.Like(p.NombreProducto.ToLower(), "%" + t + "%") ||
                (p.Codigo != null && EF.Functions.Like(p.Codigo.ToLower(), "%" + t + "%")));
        }

        var candidatos = await query
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .Include(p => p.ProProductosRetornables)
            .OrderBy(p => p.NombreProducto)
            .Take(200)
            .ToListAsync();

        var terminosNorm = terminos.Select(NormalizarTexto).ToArray();
        var resultado = candidatos
            .Where(p =>
            {
                var nombreNorm = NormalizarTexto(p.NombreProducto);
                var codigoNorm = p.Codigo != null ? NormalizarTexto(p.Codigo) : string.Empty;
                return terminosNorm.All(t => nombreNorm.Contains(t) || codigoNorm.Contains(t));
            })
            .Take(50)
            .ToList();

        return resultado;
    }

    public async Task<bool> ExisteCodigoAsync(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return false;
        var normalizado = NormalizarTexto(codigo);
        var codigos = await context.ProProductos
            .AsNoTracking()
            .Where(p => p.Codigo != null)
            .Select(p => p.Codigo)
            .ToListAsync();
        return codigos.Any(c => c != null && NormalizarTexto(c) == normalizado);
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
