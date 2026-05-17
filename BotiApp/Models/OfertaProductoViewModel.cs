using Infraestructura.Entities.BotiApp;

namespace BotiApp.Models;

public class OfertaProductoViewModel
{
    public ProProductos        Producto      { get; set; } = null!;
    public ProOfertaProducto?  OfertaActiva  { get; set; }
    public IEnumerable<ProOfertaProducto> Historial     { get; set; } = [];
    public IEnumerable<ProPromocion>      PromosActivas { get; set; } = [];
}
