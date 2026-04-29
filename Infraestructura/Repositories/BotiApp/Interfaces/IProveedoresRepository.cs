using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IProveedoresRepository
{
    Task<IEnumerable<ProProveedores>> ObtenerTodosAsync();
    Task<ProProveedores?> ObtenerPorIdAsync(int id);
    Task<ProProveedores> CrearAsync(ProProveedores proveedor);
    Task<ProProveedores> ActualizarAsync(ProProveedores proveedor);
    Task<bool> ToggleEstadoAsync(int id);

    // Productos asociados
    Task<IEnumerable<ProProveedoresProductos>> ObtenerProductosAsync(int idProveedor);
    Task<IEnumerable<ProProductos>> ObtenerProductosDisponiblesAsync(int idProveedor);
    Task<ProProveedoresProductos> AsociarProductoAsync(int idProveedor, int idProducto, int precioProveedor = 0);
    Task<bool> DesasociarProductoAsync(int idProveedorProducto);
    Task<bool> ActualizarPrecioProductoAsync(int idProveedorProducto, int precioProveedor);
}
