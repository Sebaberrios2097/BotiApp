using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Proveedores")]
public partial class ProProveedores
{
    [Key]
    [Column("Id_Proveedor")]
    public int IdProveedor { get; set; }

    [Column("Nombre_Proveedor")]
    [StringLength(150)]
    public string NombreProveedor { get; set; } = null!;

    [Column("Descripcion_Proveedor")]
    [StringLength(300)]
    public string? DescripcionProveedor { get; set; }

    public bool Estado { get; set; } = true;

    [InverseProperty("IdProveedorNavigation")]
    public virtual ICollection<ComOrdenCompra> ComOrdenCompra { get; set; } = new List<ComOrdenCompra>();

    [InverseProperty("IdProveedorNavigation")]
    public virtual ICollection<ComOrdenDetalle> ComOrdenDetalle { get; set; } = new List<ComOrdenDetalle>();

    [InverseProperty("IdProveedorNavigation")]
    public virtual ICollection<ProProveedoresDiasEntrega> ProProveedoresDiasEntrega { get; set; } = new List<ProProveedoresDiasEntrega>();

    [InverseProperty("IdProveedorNavigation")]
    public virtual ICollection<ProProveedoresProductos> ProProveedoresProductos { get; set; } = new List<ProProveedoresProductos>();
}
