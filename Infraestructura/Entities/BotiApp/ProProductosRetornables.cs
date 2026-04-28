using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Productos_Retornables")]
[Index("IdProducto", Name = "UQ_Id_Producto", IsUnique = true)]
public partial class ProProductosRetornables
{
    [Key]
    [Column("Id_Producto_Retornable")]
    public int IdProductoRetornable { get; set; }

    [Column("Id_Producto")]
    public int IdProducto { get; set; }

    [Column("Valor_Envase")]
    public int ValorEnvase { get; set; }

    [Column("Solo_Efectivo")]
    public bool SoloEfectivo { get; set; }

    [ForeignKey("IdProducto")]
    [InverseProperty("ProProductosRetornables")]
    public virtual ProProductos IdProductoNavigation { get; set; } = null!;
}
