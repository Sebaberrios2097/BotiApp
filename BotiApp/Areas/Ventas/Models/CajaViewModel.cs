using Infraestructura.Entities.BotiApp;

namespace BotiApp.Areas.Ventas.Models;

public class CajaViewModel
{
    public bool PuedeCobrar { get; set; }
    public IEnumerable<VenMetodosPago> MetodosPago { get; set; } = [];
    public IEnumerable<ProProductos> Productos { get; set; } = [];
    public IEnumerable<ProOfertaProducto> Ofertas { get; set; } = [];
    public IEnumerable<ProPromocion> Promociones { get; set; } = [];
    public SiiEmisorDto EmisorSii { get; set; } = new();
}

public class SiiEmisorDto
{
    public string Rut { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string Giro { get; init; } = string.Empty;
    public string Direccion { get; init; } = string.Empty;
    public string Comuna { get; init; } = string.Empty;
    public string Ambiente { get; init; } = string.Empty;
}
