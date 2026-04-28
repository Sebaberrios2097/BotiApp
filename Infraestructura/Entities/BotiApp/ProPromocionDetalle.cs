using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Promocion_Detalle")]
public partial class ProPromocionDetalle
{
    [Key]
    [Column("Id_Promocion_Detalle")]
    public int IdPromocionDetalle { get; set; }

    [Column("Id_Promocion")]
    public int IdPromocion { get; set; }

    [Column("Id_Producto")]
    public int IdProducto { get; set; }

    public int Cantidad { get; set; }

    [Column("Id_Grupo")]
    public int? IdGrupo { get; set; }

    [ForeignKey("IdGrupo")]
    [InverseProperty("ProPromocionDetalle")]
    public virtual ProPromocionGrupo? IdGrupoNavigation { get; set; }

    [ForeignKey("IdProducto")]
    [InverseProperty("ProPromocionDetalle")]
    public virtual ProProductos IdProductoNavigation { get; set; } = null!;

    [ForeignKey("IdPromocion")]
    [InverseProperty("ProPromocionDetalle")]
    public virtual ProPromocion IdPromocionNavigation { get; set; } = null!;
}
