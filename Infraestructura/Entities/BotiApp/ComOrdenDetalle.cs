using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Com_Orden_Detalle")]
public partial class ComOrdenDetalle
{
    [Key]
    [Column("Id_Orden_Detalle")]
    public int IdOrdenDetalle { get; set; }

    [Column("Id_Orden_Compra")]
    public int IdOrdenCompra { get; set; }

    [Column("Id_Proveedor_Producto")]
    public int IdProveedorProducto { get; set; }

    [Column("Id_Proveedor")]
    public int IdProveedor { get; set; }

    public int Cantidad { get; set; }

    [Column("Precio_Unitario")]
    public int PrecioUnitario { get; set; }

    public int Subtotal { get; set; }

    [ForeignKey("IdOrdenCompra")]
    [InverseProperty("ComOrdenDetalle")]
    public virtual ComOrdenCompra IdOrdenCompraNavigation { get; set; } = null!;

    [ForeignKey("IdProveedor")]
    [InverseProperty("ComOrdenDetalle")]
    public virtual ProProveedores IdProveedorNavigation { get; set; } = null!;

    [ForeignKey("IdProveedorProducto")]
    [InverseProperty("ComOrdenDetalle")]
    public virtual ProProveedoresProductos IdProveedorProductoNavigation { get; set; } = null!;
}
