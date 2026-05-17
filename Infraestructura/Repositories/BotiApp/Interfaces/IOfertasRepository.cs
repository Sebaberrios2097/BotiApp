using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IOfertasRepository
{
    Task<ProProductos?> ObtenerProductoAsync(int idProducto);
    Task<ProOfertaProducto?> ObtenerActivaPorProductoAsync(int idProducto);
    Task<IEnumerable<ProOfertaProducto>> ObtenerHistorialPorProductoAsync(int idProducto);
    Task<IEnumerable<ProPromocion>> ObtenerPromosActivasPorProductoAsync(int idProducto);
    Task<ProOfertaProducto> CrearAsync(ProOfertaProducto oferta);
    Task<ProOfertaProducto?> ObtenerPorIdAsync(int id);
    Task<bool> DesactivarAsync(int idOferta);
    Task<bool> ActivarAsync(int idOferta);
    Task<bool> ActualizarAsync(int idOferta, int precioOferta, DateTime fechaInicio, DateTime? fechaTermino);
}
