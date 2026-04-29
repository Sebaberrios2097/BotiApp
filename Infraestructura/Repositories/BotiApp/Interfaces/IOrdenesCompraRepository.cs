using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IOrdenesCompraRepository
{
    Task<IEnumerable<ComOrdenCompra>> ObtenerTodasAsync();
    Task<ComOrdenCompra?> ObtenerPorIdAsync(int id);
    Task<ComOrdenCompra> CrearAsync(ComOrdenCompra orden, IEnumerable<ComOrdenDetalle> detalles);
    Task<bool> CambiarEstadoAsync(int idOrden, int idEstado);

    Task<IEnumerable<ComEstadosOrdenCompra>> ObtenerEstadosAsync();
    Task<IEnumerable<ProProveedores>> ObtenerProveedoresActivosAsync();
    Task<IEnumerable<ProProveedoresProductos>> ObtenerProductosPorProveedorAsync(int idProveedor, int? idCategoria = null);
    Task<IEnumerable<ProTiposProductos>> ObtenerCategoriasConProductosAsync(int idProveedor);
    Task<bool> ActualizarPreciosOrdenAsync(int idOrden, bool incluyeIva, IEnumerable<(int idOrdenDetalle, int precioUnitario)> precios);
}
