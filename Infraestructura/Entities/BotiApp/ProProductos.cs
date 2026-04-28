using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Productos")]
public partial class ProProductos
{
    [Key]
    [Column("Id_Producto")]
    public int IdProducto { get; set; }

    [Column("Id_Tipo_Producto")]
    public int IdTipoProducto { get; set; }

    [Column("Id_Marca")]
    public int IdMarca { get; set; }

    [Column("Nombre_Producto")]
    [StringLength(100)]
    public string NombreProducto { get; set; } = null!;

    [StringLength(150)]
    public string? Descripción { get; set; }

    public int Precio { get; set; }

    public int Stock { get; set; }

    public bool Estado { get; set; }

    public byte[]? Imagen { get; set; }

    [StringLength(300)]
    public string? Codigo { get; set; }

    /// <summary>
    /// Columna que indica la fecha de ingreso del producto al sistema.
    /// </summary>
    [Column("Fecha_Ingreso", TypeName = "datetime")]
    public DateTime FechaIngreso { get; set; }

    [InverseProperty("IdProductoNavigation")]
    public virtual ICollection<AudProProductos> AudProProductos { get; set; } = new List<AudProProductos>();

    [ForeignKey("IdMarca")]
    [InverseProperty("ProProductos")]
    public virtual ProMarcas IdMarcaNavigation { get; set; } = null!;

    [ForeignKey("IdTipoProducto")]
    [InverseProperty("ProProductos")]
    public virtual ProTiposProductos IdTipoProductoNavigation { get; set; } = null!;

    [InverseProperty("IdProductoNavigation")]
    public virtual ICollection<ProOfertaProducto> ProOfertaProducto { get; set; } = new List<ProOfertaProducto>();

    [InverseProperty("IdProductoNavigation")]
    public virtual ProProductosRetornables? ProProductosRetornables { get; set; }

    [InverseProperty("IdProductoNavigation")]
    public virtual ICollection<ProPromocionDetalle> ProPromocionDetalle { get; set; } = new List<ProPromocionDetalle>();

    [InverseProperty("IdProductoNavigation")]
    public virtual ICollection<ProProveedoresProductos> ProProveedoresProductos { get; set; } = new List<ProProveedoresProductos>();

    [InverseProperty("IdProductoNavigation")]
    public virtual ICollection<VenBoletaDetalle> VenBoletaDetalle { get; set; } = new List<VenBoletaDetalle>();
}
