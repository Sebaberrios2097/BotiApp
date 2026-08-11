using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IPromocionesRepository
{
    Task<IEnumerable<ProPromocion>> ObtenerTodasAsync();
    Task<ProPromocion?> ObtenerPorIdAsync(int id);
    Task<ProPromocion> CrearAsync(ProPromocion promocion);
    Task<ProPromocion?> ActualizarAsync(ProPromocion promocion);
    Task<ProPromocion?> ToggleEstadoAsync(int id);
    Task<bool> EliminarAsync(int id);

    // Grupos
    Task<ProPromocionGrupo> CrearGrupoAsync(int idPromocion, string descripcion, bool esExcluyente = true);
    Task<bool> EliminarGrupoAsync(int idGrupo);
    Task<ProPromocionGrupo?> RenombrarGrupoAsync(int idGrupo, string descripcion, bool esExcluyente);
    Task<ProPromocionGrupo?> DuplicarGrupoAsync(int idGrupo);
    Task<ProPromocionGrupo?> ReplicarBaseEnGrupoAsync(int idPromocion, bool esExcluyente = true);

    // Detalle
    Task<ProPromocionDetalle> AgregarProductoAsync(int idPromocion, int idProducto, int cantidad, int? idGrupo = null);
    Task<bool> QuitarProductoAsync(int idPromocionDetalle);
    Task<ProPromocionDetalle?> ActualizarCantidadAsync(int idPromocionDetalle, int cantidad);
    Task<ProPromocionDetalle?> MoverDetalleAsync(int idPromocionDetalle, int? idGrupo);

    Task<IEnumerable<ProPromocion>> ObtenerUltimasAsync(int top = 5);
    Task<IEnumerable<ProProductos>> BuscarProductosAsync(string q);
    Task<IEnumerable<ProProductos>> ListarProductosActivosAsync();
}