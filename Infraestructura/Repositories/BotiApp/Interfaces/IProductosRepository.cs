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
}
