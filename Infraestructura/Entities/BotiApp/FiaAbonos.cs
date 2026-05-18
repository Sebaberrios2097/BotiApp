using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Fia_Abonos")]
public partial class FiaAbonos
{
    [Key]
    [Column("Id_Abono")]
    public int IdAbono { get; set; }

    [Column("Id_Cliente")]
    public int IdCliente { get; set; }

    [Column("Id_Usuario")]
    public int IdUsuario { get; set; }

    [Column("Monto")]
    public int Monto { get; set; }

    [Column("Fecha", TypeName = "datetime")]
    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column("Observaciones")]
    [StringLength(300)]
    public string? Observaciones { get; set; }

    [ForeignKey("IdCliente")]
    [InverseProperty("FiaAbonos")]
    public virtual FiaClientes IdClienteNavigation { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    [InverseProperty("FiaAbonos")]
    public virtual EmpUsuario IdUsuarioNavigation { get; set; } = null!;
}
