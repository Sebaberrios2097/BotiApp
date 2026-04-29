using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Proveedores_Productos")]
public partial class ProProveedoresProductos
{
    [Key]
    [Column("Id_Proveedor_Producto")]
    public int IdProveedorProducto { get; set; }

    [Column("Id_Producto")]
    public int IdProducto { get; set; }

    [Column("Id_Proveedor")]
    public int IdProveedor { get; set; }

    /// <summary>
    /// Refleja el estado del proveedor con el producto, indicando si el proveedor sigue encargado de entregar ese producto o no.
    /// </summary>
    public bool Estado { get; set; }

    [Column("Precio_Proveedor")]
    public int PrecioProveedor { get; set; }

    [Column("Fecha_Modificacion", TypeName = "datetime")]
    public DateTime? FechaModificacion { get; set; }

    [InverseProperty("IdProveedorProductoNavigation")]
    public virtual ICollection<ComOrdenDetalle> ComOrdenDetalle { get; set; } = new List<ComOrdenDetalle>();

    [ForeignKey("IdProducto")]
    [InverseProperty("ProProveedoresProductos")]
    public virtual ProProductos IdProductoNavigation { get; set; } = null!;

    [ForeignKey("IdProveedor")]
    [InverseProperty("ProProveedoresProductos")]
    public virtual ProProveedores IdProveedorNavigation { get; set; } = null!;
}
