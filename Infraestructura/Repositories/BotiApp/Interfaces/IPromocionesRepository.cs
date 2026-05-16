using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IPromocionesRepository
{
    Task<IEnumerable<ProPromocion>> ObtenerTodasAsync();
    Task<ProPromocion?> ObtenerPorIdAsync(int id);
    Task<ProPromocion> CrearAsync(ProPromocion promocion);
    Task<bool?> ToggleEstadoAsync(int id);

    // Grupos
    Task<ProPromocionGrupo> CrearGrupoAsync(int idPromocion, string descripcion, bool esExcluyente = true);
    Task<bool> EliminarGrupoAsync(int idGrupo);

    // Detalle
    Task<ProPromocionDetalle> AgregarProductoAsync(int idPromocion, int idProducto, int cantidad, int? idGrupo = null);
    Task<bool> QuitarProductoAsync(int idPromocionDetalle);

    Task<IEnumerable<ProPromocion>> ObtenerUltimasAsync(int top = 5);
    Task<IEnumerable<ProProductos>> BuscarProductosAsync(string q);
}