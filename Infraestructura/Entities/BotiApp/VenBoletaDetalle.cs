using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Ven_Boleta_Detalle")]
public partial class VenBoletaDetalle
{
    [Key]
    [Column("Id_Boleta_Detalle")]
    public int IdBoletaDetalle { get; set; }

    [Column("Id_Boleta")]
    public int IdBoleta { get; set; }

    [Column("Id_Producto")]
    public int IdProducto { get; set; }

    [Column("Id_Promocion")]
    public int? IdPromocion { get; set; }

    [Column("Id_Oferta_Producto")]
    public int? IdOfertaProducto { get; set; }

    public int Cantidad { get; set; }

    [Column("Precio_Normal")]
    public int PrecioNormal { get; set; }

    [Column("Precio_Unitario")]
    public int PrecioUnitario { get; set; }

    public int Subtotal { get; set; }

    [ForeignKey("IdBoleta")]
    [InverseProperty("VenBoletaDetalle")]
    public virtual VenBoletas IdBoletaNavigation { get; set; } = null!;

    [ForeignKey("IdProducto")]
    [InverseProperty("VenBoletaDetalle")]
    public virtual ProProductos IdProductoNavigation { get; set; } = null!;

    [ForeignKey("IdPromocion")]
    public virtual ProPromocion? IdPromocionNavigation { get; set; }

    [ForeignKey("IdOfertaProducto")]
    public virtual ProOfertaProducto? IdOfertaProductoNavigation { get; set; }
}
