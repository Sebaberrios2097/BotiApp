using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface IFiadoRepository
{
    // ── Clientes ──────────────────────────────────────────────────────────────
    Task<IEnumerable<FiaClientes>> ObtenerClientesAsync(string? q = null);
    Task<FiaClientes?> ObtenerClientePorIdAsync(int id);
    Task<FiaClientes?> ObtenerClientePorRutAsync(int rut);
    Task<FiaClientes> CrearClienteAsync(FiaClientes cliente);
    Task<FiaClientes?> ActualizarClienteAsync(FiaClientes datos);

    // ── Boletas fiadas ────────────────────────────────────────────────────────
    Task<IEnumerable<VenBoletas>> ObtenerBoletasFiadasPorClienteAsync(int idCliente);

    // ── Abonos ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Registra un abono al saldo a favor del cliente y aplica FIFO auto-pago
    /// sobre sus boletas más antiguas con estado Fiado (4).
    /// Devuelve el registro de abono creado.
    /// </summary>
    Task<FiaAbonos> RegistrarAbonoAsync(int idCliente, int idUsuario, int monto, string? observaciones = null);
    Task<IEnumerable<FiaAbonos>> ObtenerAbonosPorClienteAsync(int idCliente);

    // ── Dashboard / globales ──────────────────────────────────────────────────
    Task<int> ObtenerTotalGlobalAdeudadoAsync();
    Task<int> ObtenerCantidadClientesConDeudaAsync();
}
