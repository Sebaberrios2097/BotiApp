using Infraestructura.Entities.BotiApp;

namespace BotiApp.Api.Dtos;

/// <summary>
/// ViewModel compartido del Dashboard. Las propiedades que no apliquen al rol
/// permanecen en null / 0 y el frontend las ignora condicionalmente.
/// </summary>
public class DashboardViewModel
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string TipoUsuario { get; set; } = string.Empty;

    public int Mes { get; set; }
    public int Anio { get; set; }
    public List<(int Anio, int Mes)> PeriodosDisponibles { get; set; } = [];

    public int TotalBoletasMes { get; set; }
    public int TotalBoletasPagadasMes { get; set; }
    public int TotalBoletasAnuladasMes { get; set; }
    public int TotalBoletasPendientesMes { get; set; }
    public int TotalBoletasFiadasMes { get; set; }
    public long MontoTotalMes { get; set; }
    public int TotalOrdenesPendientes { get; set; }
    public int TotalProductosBajoStock { get; set; }

    public long[] VentasPorDiaMes { get; set; } = [];
    public Dictionary<string, long> MontosPorMetodoPago { get; set; } = [];
    public List<ProProductos> ProductosBajoStock { get; set; } = [];

    public IEnumerable<VenBoletas> UltimasBoletas { get; set; } = [];

    public int VendedorBoletasMes { get; set; }
    public long VendedorMontoMes { get; set; }
    public int VendedorBoletasPendientes { get; set; }
    public IEnumerable<VenBoletas> VendedorUltimasBoletas { get; set; } = [];

    public int CajeroBoletasCobradas { get; set; }
    public int CajeroBoletasAnuladas { get; set; }
    public long CajeroMontoGestionado { get; set; }
    public IEnumerable<VenBoletas> CajeroUltimasBoletas { get; set; } = [];

    public int TotalFiadoGlobal { get; set; }
    public int CantFiadosActivos { get; set; }

    public Dictionary<string, long> VentasPorCategoria { get; set; } = [];
    public List<ProductoVendidoViewModel> ProductosMasVendidos { get; set; } = [];
    public List<ProductoVendidoViewModel> ProductosMenosVendidos { get; set; } = [];

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
