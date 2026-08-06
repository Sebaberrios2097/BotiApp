using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IPromocionesRepository
{
    Task<IEnumerable<ProPromocion>> ObtenerTodasAsync();
    Task<ProPromocion?> ObtenerPorIdAsync(int id);
    Task<ProPromocion> CrearAsync(ProPromocion promocion);
    Task<bool?> ToggleEstadoAsync(int id);
    Task<bool> EliminarAsync(int id);

    // Grupos
    Task<ProPromocionGrupo> CrearGrupoAsync(int idPromocion, string descripcion, bool esExcluyente = true);
    Task<bool> EliminarGrupoAsync(int idGrupo);
    Task<ProPromocionGrupo?> RenombrarGrupoAsync(int idGrupo, string descripcion, bool esExcluyente);

    // Detalle
    Task<ProPromocionDetalle> AgregarProductoAsync(int idPromocion, int idProducto, int cantidad, int? idGrupo = null);
    Task<bool> QuitarProductoAsync(int idPromocionDetalle);

    Task<IEnumerable<ProPromocion>> ObtenerUltimasAsync(int top = 5);
    Task<IEnumerable<ProProductos>> BuscarProductosAsync(string q);
    Task<IEnumerable<ProProductos>> ListarProductosActivosAsync();
}