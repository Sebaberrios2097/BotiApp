using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Producto_Pack")]
[Index("IdProductoUnidad", Name = "UQ_Pro_Producto_Pack_Unidad", IsUnique = true)]
public partial class ProProductoPack
{
    [Key]
    [Column("Id_Producto_Pack")]
    public int IdProductoPack { get; set; }

    [Column("Id_Producto_Pack_Producto")]
    public int IdProductoPackProducto { get; set; }

    [Column("Id_Producto_Unidad")]
    public int IdProductoUnidad { get; set; }

    [Column("Cantidad_Unidades")]
    public int CantidadUnidades { get; set; }

    [Column("Fecha_Creacion", TypeName = "datetime")]
    public DateTime FechaCreacion { get; set; }

    [Column("Estado")]
    public bool Estado { get; set; }

    [ForeignKey("IdProductoPackProducto")]
    [InverseProperty("ProProductoPackComoPack")]
    public virtual ProProductos IdProductoPackProductoNavigation { get; set; } = null!;

    [ForeignKey("IdProductoUnidad")]
    [InverseProperty("ProProductoPackComoUnidad")]
    public virtual ProProductos IdProductoUnidadNavigation { get; set; } = null!;
}
