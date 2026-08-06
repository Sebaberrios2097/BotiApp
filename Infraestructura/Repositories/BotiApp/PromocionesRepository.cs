using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class PromocionesRepository(BotiAppContext context) : IPromocionesRepository
{
    public async Task<IEnumerable<ProPromocion>> ObtenerTodasAsync()
        => await context.ProPromocion
            .Include(p => p.ProPromocionGrupo)
            .Include(p => p.ProPromocionDetalle)
                .ThenInclude(d => d.IdGrupoNavigation)
            .Include(p => p.ProPromocionDetalle)
                .ThenInclude(d => d.IdProductoNavigation)
                    .ThenInclude(pr => pr.IdMarcaNavigation)
            .Include(p => p.ProPromocionDetalle)
                .ThenInclude(d => d.IdProductoNavigation)
                    .ThenInclude(pr => pr.IdTipoProductoNavigation)
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync();

    public async Task<ProPromocion?> ObtenerPorIdAsync(int id)
        => await context.ProPromocion
            .Include(p => p.ProPromocionGrupo)
            .Include(p => p.ProPromocionDetalle)
                .ThenInclude(d => d.IdGrupoNavigation)
            .Include(p => p.ProPromocionDetalle)
                .ThenInclude(d => d.IdProductoNavigation)
                    .ThenInclude(pr => pr.IdMarcaNavigation)
            .Include(p => p.ProPromocionDetalle)
                .ThenInclude(d => d.IdProductoNavigation)
                    .ThenInclude(pr => pr.IdTipoProductoNavigation)
            .FirstOrDefaultAsync(p => p.IdPromocion == id);

    public async Task<ProPromocion> CrearAsync(ProPromocion promocion)
    {
        context.ProPromocion.Add(promocion);
        await context.SaveChangesAsync();
        return promocion;
    }

    public async Task<bool?> ToggleEstadoAsync(int id)
    {
        var promo = await context.ProPromocion.FindAsync(id);
        if (promo is null) return null;
        promo.Estado = !promo.Estado;
        await context.SaveChangesAsync();
        return promo.Estado;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var promo = await context.ProPromocion
            .Include(p => p.ProPromocionGrupo)
            .Include(p => p.ProPromocionDetalle)
            .FirstOrDefaultAsync(p => p.IdPromocion == id);
        if (promo is null) return false;

        if (promo.ProPromocionDetalle.Any())
            context.ProPromocionDetalle.RemoveRange(promo.ProPromocionDetalle);
        if (promo.ProPromocionGrupo.Any())
            context.ProPromocionGrupo.RemoveRange(promo.ProPromocionGrupo);

        context.ProPromocion.Remove(promo);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<ProPromocionGrupo> CrearGrupoAsync(
        int idPromocion, string descripcion, bool esExcluyente = true)
    {
        var grupo = new ProPromocionGrupo
        {
            IdPromocion = idPromocion,
            Descripcion = descripcion,
            EsExcluyente = esExcluyente
        };
        context.ProPromocionGrupo.Add(grupo);
        await context.SaveChangesAsync();
        return grupo;
    }

    public async Task<bool> EliminarGrupoAsync(int idGrupo)
    {
        var grupo = await context.ProPromocionGrupo
            .Include(g => g.ProPromocionDetalle)
            .FirstOrDefaultAsync(g => g.IdGrupo == idGrupo);
        if (grupo is null) return false;
        // Al eliminar el grupo también se eliminan los productos asociados
        if (grupo.ProPromocionDetalle.Any())
            context.ProPromocionDetalle.RemoveRange(grupo.ProPromocionDetalle);
        context.ProPromocionGrupo.Remove(grupo);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<ProPromocionGrupo?> RenombrarGrupoAsync(int idGrupo, string descripcion, bool esExcluyente)
    {
        var grupo = await context.ProPromocionGrupo.FindAsync(idGrupo);
        if (grupo is null) return null;
        grupo.Descripcion  = descripcion.Trim();
        grupo.EsExcluyente = esExcluyente;
        await context.SaveChangesAsync();
        return grupo;
    }

    public async Task<ProPromocionDetalle> AgregarProductoAsync(
        int idPromocion, int idProducto, int cantidad, int? idGrupo = null)
    {
        var detalle = new ProPromocionDetalle
        {
            IdPromocion = idPromocion,
            IdProducto = idProducto,
            Cantidad = cantidad,
            IdGrupo = idGrupo
        };
        context.ProPromocionDetalle.Add(detalle);
        await context.SaveChangesAsync();

        return await context.ProPromocionDetalle
            .Include(d => d.IdProductoNavigation)
                .ThenInclude(p => p.IdMarcaNavigation)
            .Include(d => d.IdProductoNavigation)
                .ThenInclude(p => p.IdTipoProductoNavigation)
            .Include(d => d.IdGrupoNavigation)
            .FirstAsync(d => d.IdPromocionDetalle == detalle.IdPromocionDetalle);
    }

    public async Task<bool> QuitarProductoAsync(int idPromocionDetalle)
    {
        var detalle = await context.ProPromocionDetalle.FindAsync(idPromocionDetalle);
        if (detalle is null) return false;
        context.ProPromocionDetalle.Remove(detalle);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ProPromocion>> ObtenerUltimasAsync(int top = 5)
        => await context.ProPromocion
            .OrderByDescending(p => p.FechaInicio)
            .Take(top)
            .ToListAsync();

    public async Task<IEnumerable<ProProductos>> BuscarProductosAsync(string q)
    {
        q = (q ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(q))
            return Enumerable.Empty<ProProductos>();

        var filtroNorm = NormalizarTexto(q);

        var todos = await context.ProProductos
            .AsNoTracking()
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .Where(p => p.Estado)
            .ToListAsync();

        return todos
            .Where(p =>
                NormalizarTexto(p.NombreProducto).Contains(filtroNorm) ||
                (p.Codigo != null && NormalizarTexto(p.Codigo).Contains(filtroNorm)))
            .OrderBy(p => p.NombreProducto)
            .Take(20);
    }

    public async Task<IEnumerable<ProProductos>> ListarProductosActivosAsync()
        => await context.ProProductos
            .AsNoTracking()
            .Include(p => p.IdMarcaNavigation)
            .Include(p => p.IdTipoProductoNavigation)
            .Where(p => p.Estado)
            .OrderBy(p => p.NombreProducto)
            .ToListAsync();

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
}