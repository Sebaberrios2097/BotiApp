using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces;

/// <summary>Representa un método de pago con su monto para el registro de abonos multi-método.</summary>
public record AbonoMetodoItem(int IdMetodoPago, int Monto);

/// <summary>Resultado de desactivar, reactivar o eliminar un cliente fiado.</summary>
public record ResultadoBajaCliente(bool Ok, string Mensaje);

public interface IFiadoRepository
{
    // ── Clientes ──────────────────────────────────────────────────────────────
    /// <param name="soloActivos">false incluye los desactivados (solo para administrarlos).</param>
    Task<IEnumerable<FiaClientes>> ObtenerClientesAsync(string? q = null, bool soloActivos = true);
    Task<FiaClientes?> ObtenerClientePorIdAsync(int id);

    Task<FiaClientes> CrearClienteAsync(FiaClientes cliente);
    Task<FiaClientes?> ActualizarClienteAsync(FiaClientes datos);

    // ── Baja de clientes ──────────────────────────────────────────────────────
    /// <summary>Desactiva al cliente. Falla si tiene boletas fiadas sin pagar.</summary>
    Task<ResultadoBajaCliente> DesactivarClienteAsync(int idCliente);
    Task<ResultadoBajaCliente> ReactivarClienteAsync(int idCliente);
    /// <summary>
    /// Borrado definitivo: exige que el cliente esté desactivado y sin deuda. Elimina
    /// sus abonos y deja sus boletas sin cliente asociado. Irreversible.
    /// </summary>
    Task<ResultadoBajaCliente> EliminarClienteAsync(int idCliente);

    // ── Boletas fiadas ────────────────────────────────────────────────────────
    /// <summary>Solo estado Fiado (4) — para cálculo de deuda activa.</summary>
    Task<IEnumerable<VenBoletas>> ObtenerBoletasFiadasPorClienteAsync(int idCliente);
    /// <summary>Todas las boletas del cliente (cualquier estado) — para historial.</summary>
    Task<IEnumerable<VenBoletas>> ObtenerBoletasHistorialAsync(int idCliente);

    // ── Abonos ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Registra N abonos (uno por método de pago) al saldo a favor del cliente
    /// y aplica FIFO auto-pago sobre sus boletas más antiguas con estado Fiado (4).
    /// Devuelve la lista de registros de abono creados.
    /// </summary>
    Task<IEnumerable<FiaAbonos>> RegistrarAbonoAsync(int idCliente, int idUsuario, IEnumerable<AbonoMetodoItem> metodos, string? observaciones = null);
    Task<IEnumerable<FiaAbonos>> ObtenerAbonosPorClienteAsync(int idCliente);

    // ── Dashboard / globales ──────────────────────────────────────────────────
    Task<int> ObtenerTotalGlobalAdeudadoAsync();
    Task<int> ObtenerCantidadClientesConDeudaAsync();
}
