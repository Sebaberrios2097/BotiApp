using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IProductosRepository
{
    Task<IEnumerable<ProProductos>> ObtenerTodosAsync();
    Task<ProProductos?> ObtenerPorIdAsync(int id);
    Task<ProProductos> CrearAsync(ProProductos producto);
    Task<ProProductos> ActualizarAsync(ProProductos producto);
    Task<bool> EliminarAsync(int id);
    Task<bool> ToggleEstadoAsync(int id);
    Task<IEnumerable<ProMarcas>> ObtenerMarcasAsync();
    Task<IEnumerable<ProTiposProductos>> ObtenerTiposProductosAsync();
    Task<IEnumerable<AudProProductos>> ObtenerAuditoriaAsync(int idProducto, int top = 6);
    Task<IEnumerable<ProProductos>> ObtenerUltimosIngresadosAsync(int top = 5);
    // Retornables
    Task<IEnumerable<ProProductosRetornables>> ObtenerRetornablesAsync();
    Task<ProProductosRetornables> AgregarRetornableAsync(ProProductosRetornables retornable);
    Task<bool> EliminarRetornableAsync(int idProducto);
    // Pack de un producto
    Task<ProProductoPack?> ObtenerPackPorProductoAsync(int idProducto);
    Task<ProProductoPack> UpsertPackAsync(ProProductoPack pack);
    Task<bool> EliminarPackPorProductoAsync(int idProducto);
    /// <summary>
    /// Ids de los productos que ya están definidos como pack. Se usan para impedir que
    /// un pack sea a su vez la unidad base de otro (el stock no propaga en cadena).
    /// </summary>
    Task<HashSet<int>> ObtenerIdsProductosQueSonPackAsync();

    /// <summary>Packs que se arman con esa unidad base. Puede haber varios de distinto tamaño.</summary>
    Task<List<ProProductoPack>> ObtenerPacksPorUnidadAsync(int idUnidad);
    // Aplica un delta al stock de un producto manteniendo pack/unidad sincronizados.
    Task AplicarDeltaStockAsync(int idProducto, int delta);
    // Búsqueda por nombre o código (coincidencia parcial, ignora tildes/case)
    Task<IEnumerable<ProProductos>> BuscarAsync(string filtro);
    // Verifica si ya existe un producto con un código determinado
    Task<bool> ExisteCodigoAsync(string codigo);
    // Creación de Marca / Tipo
    Task<ProMarcas> CrearMarcaAsync(ProMarcas marca);
    Task<ProTiposProductos> CrearTipoProductoAsync(ProTiposProductos tipo);
}
