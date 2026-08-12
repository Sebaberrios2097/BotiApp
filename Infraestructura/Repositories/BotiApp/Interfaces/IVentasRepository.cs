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

    // ── Ventas postergadas (estado 5 «Sin Finalizar») ─────────────────────────
    /// <summary>
    /// Guarda el carrito como venta postergada: estado 5, sin correlativo diario y
    /// sin mover stock, porque todavía no es una venta emitida.
    /// </summary>
    Task<VenBoletas> PostergarVentaAsync(VenBoletas boleta, IEnumerable<VenBoletaDetalle> detalles);
    /// <summary>
    /// Reemplaza el detalle de una venta ya postergada conservando su identidad.
    /// Null si no existe o dejó de estar postergada.
    /// </summary>
    Task<VenBoletas?> ActualizarVentaPostergadaAsync(int idBoleta, int montoTotal, IEnumerable<VenBoletaDetalle> detalles);
    /// <summary>
    /// Emite una venta postergada reutilizando su boleta y su correlativo reservado,
    /// descontando el stock. Null si ya no existe o dejó de estar postergada.
    /// </summary>
    Task<VenBoletas?> EmitirVentaPostergadaAsync(int idBoleta, int idVendedor, IEnumerable<VenBoletaDetalle> detalles);
    Task<IEnumerable<VenBoletas>> ObtenerVentasPostergadasAsync(int top = 30);
    /// <summary>
    /// Devuelve la venta postergada con su detalle sin eliminarla, para cargarla en el
    /// carrito conservándola en el panel. Null si no existe o no está postergada.
    /// </summary>
    Task<VenBoletas?> RecuperarVentaPostergadaAsync(int idBoleta);
    /// <summary>
    /// Elimina la venta postergada y devuelve su número visible, o null si ya no existía.
    /// </summary>
    Task<int?> DescartarVentaPostergadaAsync(int idBoleta);

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
