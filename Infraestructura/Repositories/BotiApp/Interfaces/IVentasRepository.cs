using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IVentasRepository
{
    // ── Boletas ───────────────────────────────────────────────────────────────
    Task<IEnumerable<VenBoletas>> ObtenerBoletasAsync();
    Task<IEnumerable<VenBoletas>> ObtenerBoletasPorUsuarioAsync(int idUsuario, int top = 15);
    Task<IEnumerable<VenBoletas>> ObtenerUltimasBoletasSistemaAsync(int top = 15);
    Task<VenBoletas?> ObtenerPorIdAsync(int id);
    Task<VenBoletas> CrearBoletaAsync(VenBoletas boleta, IEnumerable<VenBoletaDetalle> detalles);

    // ── Catálogo ──────────────────────────────────────────────────────────────
    Task<IEnumerable<ProProductos>> ObtenerProductosDisponiblesAsync();
    Task<IEnumerable<ProTiposProductos>> ObtenerTiposAsync();
    Task<IEnumerable<ProMarcas>> ObtenerMarcasAsync();

    // ── Promociones y ofertas ─────────────────────────────────────────────────
    Task<IEnumerable<ProPromocion>> ObtenerPromocionesActivasAsync();
    Task<IEnumerable<ProOfertaProducto>> ObtenerOfertasActivasAsync();

    // ── Métodos de pago ───────────────────────────────────────────────────────
    Task<IEnumerable<VenMetodosPago>> ObtenerMetodosPagoAsync();
}