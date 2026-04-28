using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Aud_Pro_Productos")]
public partial class AudProProductos
{
    [Key]
    [Column("Id_Aud_Producto")]
    public int IdAudProducto { get; set; }

    [Column("Id_Producto")]
    public int IdProducto { get; set; }

    [Column("Nombre_Producto")]
    [StringLength(100)]
    public string NombreProducto { get; set; } = null!;

    [StringLength(150)]
    public string? Descripcion { get; set; }

    public int Precio { get; set; }

    public bool Estado { get; set; }

    [Column("Fecha_Modificacion", TypeName = "datetime")]
    public DateTime FechaModificacion { get; set; }

    [StringLength(300)]
    public string Motivo { get; set; } = null!;

    [Column("Rut_Modificador")]
    public int RutModificador { get; set; }

    [ForeignKey("IdProducto")]
    [InverseProperty("AudProProductos")]
    public virtual ProProductos IdProductoNavigation { get; set; } = null!;
}
