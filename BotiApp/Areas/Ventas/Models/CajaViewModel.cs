using Infraestructura.Entities.BotiApp;

namespace BotiApp.Areas.Ventas.Models;

public class CajaViewModel
{
    public bool PuedeCobrar { get; set; }
    public IEnumerable<VenMetodosPago> MetodosPago { get; set; } = [];
    public IEnumerable<ProProductos> Productos { get; set; } = [];
    public IEnumerable<ProOfertaProducto> Ofertas { get; set; } = [];
}
