using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Oferta_Producto")]
public partial class ProOfertaProducto
{
    [Key]
    [Column("Id_Oferta_Producto")]
    public int IdOfertaProducto { get; set; }

    [Column("Id_Producto")]
    public int IdProducto { get; set; }

    [Column("Precio_Oferta")]
    public int PrecioOferta { get; set; }

    [Column("Fecha_Inicio_Oferta", TypeName = "datetime")]
    public DateTime FechaInicioOferta { get; set; }

    [Column("Fecha_Termino_Oferta", TypeName = "datetime")]
    public DateTime? FechaTerminoOferta { get; set; }

    public bool Estado { get; set; }

    [ForeignKey("IdProducto")]
    [InverseProperty("ProOfertaProducto")]
    public virtual ProProductos IdProductoNavigation { get; set; } = null!;
}
