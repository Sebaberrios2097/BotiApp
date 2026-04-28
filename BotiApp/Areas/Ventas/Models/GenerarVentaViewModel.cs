using Infraestructura.Entities.BotiApp;

namespace BotiApp.Areas.Ventas.Models;

public class GenerarVentaViewModel
{
    public string NombreVendedor { get; set; } = string.Empty;
    public IEnumerable<ProProductos> Productos { get; set; } = [];
    public IEnumerable<ProTiposProductos> Tipos { get; set; } = [];
    public IEnumerable<ProMarcas> Marcas { get; set; } = [];
    public IEnumerable<ProPromocion> Promociones { get; set; } = [];
    public IEnumerable<ProOfertaProducto> Ofertas { get; set; } = [];
}
