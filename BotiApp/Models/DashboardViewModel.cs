using Infraestructura.Entities.BotiApp;

namespace BotiApp.Models;

/// <summary>
/// ViewModel compartido del Dashboard. Las propiedades que no apliquen al rol
/// permanecen en null / 0 y la vista las ignora condicionalmente.
/// </summary>
public class DashboardViewModel
{
    // ── Datos de contexto ──────────────────────────────────────────────────
    public string NombreUsuario { get; set; } = string.Empty;
    public string TipoUsuario   { get; set; } = string.Empty;

    // ── Tarjetas de resumen (admin) ────────────────────────────────────────
    public int TotalBoletasMes          { get; set; }
    public int TotalBoletasPagadasMes   { get; set; }
    public int TotalBoletasAnuladasMes  { get; set; }
    public int TotalBoletasPendientesMes{ get; set; }
    public long MontoTotalMes           { get; set; }
    public int TotalOrdenesPendientes   { get; set; }
    public int TotalProductosBajoStock  { get; set; }

    // ── Últimas boletas del sistema (admin) ────────────────────────────────
    public IEnumerable<VenBoletas> UltimasBoletas { get; set; } = [];

    // ── Vendedor: resumen del mes ──────────────────────────────────────────
    public int VendedorBoletasMes    { get; set; }
    public long VendedorMontoMes     { get; set; }
    public int VendedorBoletasPendientes { get; set; }
    public IEnumerable<VenBoletas> VendedorUltimasBoletas { get; set; } = [];

    // ── Cajero: resumen del mes ────────────────────────────────────────────
    public int CajeroBoletasCobradas { get; set; }
    public int CajeroBoletasAnuladas { get; set; }
    public long CajeroMontoGestionado{ get; set; }
    public IEnumerable<VenBoletas> CajeroUltimasBoletas { get; set; } = [];
}
