using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IVentasRepository
{
    // ── Boletas ───────────────────────────────────────────────────────────────
    Task<IEnumerable<VenBoletas>> ObtenerBoletasAsync();
    Task<IEnumerable<VenBoletas>> ObtenerBoletasPorVendedorAsync(int idVendedor, int top = 100);
    Task<IEnumerable<VenBoletas>> ObtenerBoletasPorCajeroAsync(int idCajero, int top = 100);
    Task<IEnumerable<VenBoletas>> ObtenerBoletasPendientesAsync(int top = 20);
    Task<VenBoletas?> ObtenerPorIdAsync(int id);
    Task<VenBoletas?> ObtenerBoletaParaCajaAsync(int id);
    Task<VenBoletas?> ObtenerBoletaPorCorrelativoDiarioAsync(int correlativo, DateTime fecha);
    Task<VenBoletas> CrearBoletaAsync(VenBoletas boleta, IEnumerable<VenBoletaDetalle> detalles);
    Task<VenBoletas?> ModificarBoletaDetalleAsync(int idBoleta, IEnumerable<VenBoletaDetalle> nuevosDetalles);
    Task<VenBoletas?> CobrarBoletaAsync(int idBoleta, int idCajero, IEnumerable<VenMetodosPagoBoleta> metodos);
    /// <summary>Cambia una boleta Pendiente (1) a estado Fiado (4) y la asocia al cliente.</summary>
    Task<VenBoletas?> DejarFiadoAsync(int idBoleta, int idClienteFiado, int idCajero);
    /// <summary>
    /// Anula una boleta Pendiente (1), Pagada (3) o Fiado (4) y devuelve el stock.
    /// Conserva al cajero que la cobró; <paramref name="nota"/> se agrega a Observaciones
    /// como traza de quién anuló y por qué.
    /// </summary>
    Task<bool> AnularBoletaAsync(int idBoleta, int idUsuario, string? nota = null);

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
    Task<IEnumerable<(int Anio, int Mes)>> ObtenerPeriodosConMovimientoAsync();
}
