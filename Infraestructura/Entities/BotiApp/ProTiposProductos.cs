using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Tipos_Productos")]
public partial class ProTiposProductos
{
    [Key]
    [Column("Id_Tipo_Producto")]
    public int IdTipoProducto { get; set; }

    [Column("Nombre_Tipo_Producto")]
    [StringLength(100)]
    public string NombreTipoProducto { get; set; } = null!;

    [InverseProperty("IdTipoProductoNavigation")]
    public virtual ICollection<ProProductos> ProProductos { get; set; } = new List<ProProductos>();
}
