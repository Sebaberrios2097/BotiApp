using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Emp_Accesos_Usuario")]
public partial class EmpAccesosUsuario
{
    [Key]
    [Column("Id_Acceso_Usuario")]
    public int IdAccesoUsuario { get; set; }

    [Column("Id_Usuario")]
    public int IdUsuario { get; set; }

    [Column("Fecha_Acceso", TypeName = "datetime")]
    public DateTime FechaAcceso { get; set; }

    [ForeignKey("IdUsuario")]
    [InverseProperty("EmpAccesosUsuario")]
    public virtual EmpUsuario IdUsuarioNavigation { get; set; } = null!;
}
