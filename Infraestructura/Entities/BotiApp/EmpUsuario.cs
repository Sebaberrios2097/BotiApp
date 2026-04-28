using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Emp_Usuario")]
[Index("IdEmpleado", Name = "UQ_Id_Empleado", IsUnique = true)]
public partial class EmpUsuario
{
    [Key]
    [Column("Id_Usuario")]
    public int IdUsuario { get; set; }

    [Column("Id_Empleado")]
    public int IdEmpleado { get; set; }

    [Column("Id_Tipo_Usuario")]
    public int IdTipoUsuario { get; set; }

    [Column("Nombre_Usuario")]
    [StringLength(50)]
    public string NombreUsuario { get; set; } = null!;

    [Column("Clave_Usuario")]
    [StringLength(300)]
    public string ClaveUsuario { get; set; } = null!;

    public bool Estado { get; set; }

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<ComOrdenCompra> ComOrdenCompra { get; set; } = new List<ComOrdenCompra>();

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<EmpAccesosUsuario> EmpAccesosUsuario { get; set; } = new List<EmpAccesosUsuario>();

    [ForeignKey("IdEmpleado")]
    [InverseProperty("EmpUsuario")]
    public virtual EmpEmpleado IdEmpleadoNavigation { get; set; } = null!;

    [ForeignKey("IdTipoUsuario")]
    [InverseProperty("EmpUsuario")]
    public virtual EmpTiposUsuario IdTipoUsuarioNavigation { get; set; } = null!;

    [InverseProperty("IdVendedorNavigation")]
    public virtual ICollection<VenBoletas> VenBoletasVendedor { get; set; } = new List<VenBoletas>();

    [InverseProperty("IdCajeroNavigation")]
    public virtual ICollection<VenBoletas> VenBoletasCajero { get; set; } = new List<VenBoletas>();
}
