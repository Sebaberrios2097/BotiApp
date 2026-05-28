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

    // ── Período seleccionado y períodos con actividad ──────────────────────
    public int Mes  { get; set; }
    public int Anio { get; set; }
    /// <summary>Meses (desc) que tienen al menos una boleta emitida.</summary>
    public List<(int Anio, int Mes)> PeriodosDisponibles { get; set; } = [];

    // ── Tarjetas de resumen (admin) ────────────────────────────────────────
    public int TotalBoletasMes          { get; set; }
    public int TotalBoletasPagadasMes   { get; set; }
    public int TotalBoletasAnuladasMes  { get; set; }
    public int TotalBoletasPendientesMes{ get; set; }
    public int TotalBoletasFiadasMes    { get; set; }
    public long MontoTotalMes           { get; set; }
    public int TotalOrdenesPendientes   { get; set; }
    public int TotalProductosBajoStock  { get; set; }

    // ── Gráficos y datos expandibles (admin) ──────────────────────────────
    /// <summary>Montos de ventas pagadas agrupados por día del mes (índice 0 = día 1).</summary>
    public long[] VentasPorDiaMes { get; set; } = [];
    /// <summary>Monto total cobrado agrupado por nombre de método de pago.</summary>
    public Dictionary<string, long> MontosPorMetodoPago { get; set; } = [];
    /// <summary>Lista de productos activos con stock ≤ 5.</summary>
    public List<ProProductos> ProductosBajoStock { get; set; } = [];

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

    // ── Fiados: globales (admin / cajero) ──────────────────────────────────
    public int TotalFiadoGlobal   { get; set; }
    public int CantFiadosActivos  { get; set; }

    // ── Nuevas propiedades para Categorías y Productos (admin) ───────────────
    public Dictionary<string, long> VentasPorCategoria { get; set; } = [];
    public List<ProductoVendidoViewModel> ProductosMasVendidos { get; set; } = [];
    public List<ProductoVendidoViewModel> ProductosMenosVendidos { get; set; } = [];

    // ── Nuevas propiedades para Cigarros (admin) ────────────────────────────
    public int CantidadCigarrosVendidos { get; set; }
    public long MontoCigarrosVendidos { get; set; }
    public List<MetodoPagoCigarrosViewModel> MetodosPagoCigarros { get; set; } = [];
}

public class ProductoVendidoViewModel
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public long Monto { get; set; }
}

public class MetodoPagoCigarrosViewModel
{
    public string MetodoPago { get; set; } = string.Empty;
    public long Monto { get; set; }
    public double Porcentaje { get; set; }
}

