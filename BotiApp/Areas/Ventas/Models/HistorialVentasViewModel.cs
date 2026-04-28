namespace BotiApp.Areas.Ventas.Models;

public class HistorialVentasViewModel
{
    public bool EsAdmin { get; set; }
    public bool EsCajero { get; set; }
    public int IdUsuarioActual { get; set; }
    public string NombreUsuarioActual { get; set; } = string.Empty;
    public IEnumerable<BoletaResumenDto> Boletas { get; set; } = [];
    public IEnumerable<VendedorFiltroDto> Vendedores { get; set; } = [];
}

public record BoletaResumenDto(
    int IdBoleta,
    string FechaEmision,
    string Estado,
    int IdEstado,
    int IdVendedor,
    string Vendedor,
    int? IdCajero,
    string? Cajero,
    int MontoTotal,
    IEnumerable<DetalleBoletaDto> Detalle
);

/// <param name="PrecioNormal">Precio regular del producto (para mostrar tachado cuando aplique descuento).</param>
/// <param name="NombrePromocion">Nombre de la promoción aplicada, o null si no aplica.</param>
/// <param name="NombreOferta">Texto "Oferta" si el producto fue comprado con oferta, o null.</param>
public record DetalleBoletaDto(string Nombre, int Cantidad, int PrecioUnitario, int PrecioNormal, int Subtotal, string? NombrePromocion, string? NombreOferta);
public record VendedorFiltroDto(int IdUsuario, string Nombre);
