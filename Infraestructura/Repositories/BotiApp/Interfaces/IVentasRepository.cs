using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IVentasRepository
{
    // ── Boletas ───────────────────────────────────────────────────────────────
    Task<IEnumerable<VenBoletas>> ObtenerBoletasAsync();
    Task<IEnumerable<VenBoletas>> ObtenerBoletasPorVendedorAsync(int idVendedor, int top = 100);
    Task<IEnumerable<VenBoletas>> ObtenerBoletasPorCajeroAsync(int idCajero, int top = 100);
    Task<VenBoletas?> ObtenerPorIdAsync(int id);
    Task<VenBoletas?> ObtenerBoletaParaCajaAsync(int id);
    Task<VenBoletas> CrearBoletaAsync(VenBoletas boleta, IEnumerable<VenBoletaDetalle> detalles);
    Task<VenBoletas?> ModificarBoletaDetalleAsync(int idBoleta, IEnumerable<VenBoletaDetalle> nuevosDetalles);
    Task<VenBoletas?> CobrarBoletaAsync(int idBoleta, int idCajero, IEnumerable<VenMetodosPagoBoleta> metodos);
    Task<bool> AnularBoletaAsync(int idBoleta, int idUsuario);

    // ── Catálogo ──────────────────────────────────────────────────────────────
    Task<IEnumerable<ProProductos>> ObtenerProductosDisponiblesAsync();
    Task<IEnumerable<ProTiposProductos>> ObtenerTiposAsync();
    Task<IEnumerable<ProMarcas>> ObtenerMarcasAsync();

    // ── Promociones y ofertas ─────────────────────────────────────────────────
    Task<IEnumerable<ProPromocion>> ObtenerPromocionesActivasAsync();
    Task<IEnumerable<ProOfertaProducto>> ObtenerOfertasActivasAsync();

    // ── Métodos de pago ───────────────────────────────────────────────────────
    Task<IEnumerable<VenMetodosPago>> ObtenerMetodosPagoAsync();

    // ── Dashboard ─────────────────────────────────────────────────────────────
    Task<IEnumerable<VenBoletas>> ObtenerBoletasDelMesAsync(int anio, int mes);
    Task<IEnumerable<VenBoletas>> ObtenerBoletasVendedorDelMesAsync(int idVendedor, int anio, int mes);
    Task<IEnumerable<VenBoletas>> ObtenerBoletasCajeroDelMesAsync(int idCajero, int anio, int mes);
}
