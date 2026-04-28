using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Com_Orden_Compra")]
public partial class ComOrdenCompra
{
    [Key]
    [Column("Id_Orden_Compra")]
    public int IdOrdenCompra { get; set; }

    [Column("Id_Usuario")]
    public int IdUsuario { get; set; }

    [Column("Id_Proveedor")]
    public int IdProveedor { get; set; }

    [Column("Id_Estado_Orden_Compra")]
    public int IdEstadoOrdenCompra { get; set; }

    [Column("Cantidad_Productos")]
    public int CantidadProductos { get; set; }

    [Column("Monto_Total")]
    public int MontoTotal { get; set; }

    [Column("Fecha_Solicitud", TypeName = "datetime")]
    public DateTime? FechaSolicitud { get; set; }

    [Column("Fecha_Llegada_Pedido", TypeName = "datetime")]
    public DateTime? FechaLlegadaPedido { get; set; }

    [InverseProperty("IdOrdenCompraNavigation")]
    public virtual ICollection<ComOrdenDetalle> ComOrdenDetalle { get; set; } = new List<ComOrdenDetalle>();

    [ForeignKey("IdEstadoOrdenCompra")]
    [InverseProperty("ComOrdenCompra")]
    public virtual ComEstadosOrdenCompra IdEstadoOrdenCompraNavigation { get; set; } = null!;

    [ForeignKey("IdProveedor")]
    [InverseProperty("ComOrdenCompra")]
    public virtual ProProveedores IdProveedorNavigation { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    [InverseProperty("ComOrdenCompra")]
    public virtual EmpUsuario IdUsuarioNavigation { get; set; } = null!;
}
