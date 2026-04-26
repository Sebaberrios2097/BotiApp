using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Emp_Empleado")]
public partial class EmpEmpleado
{
    [Key]
    [Column("Id_Empleado")]
    public int IdEmpleado { get; set; }

    [Column("Nombres_Empleado")]
    [StringLength(80)]
    public string NombresEmpleado { get; set; } = null!;

    [Column("Apellido_1")]
    [StringLength(50)]
    public string Apellido1 { get; set; } = null!;

    [Column("Apellido_2")]
    [StringLength(50)]
    public string? Apellido2 { get; set; }

    public int Rut { get; set; }

    [StringLength(50)]
    public string? Fono { get; set; }

    [StringLength(150)]
    public string? Correo { get; set; }

    [Column("Fecha_Ingreso", TypeName = "datetime")]
    public DateTime FechaIngreso { get; set; }

    [InverseProperty("IdEmpleadoNavigation")]
    public virtual EmpUsuario? EmpUsuario { get; set; }
}
