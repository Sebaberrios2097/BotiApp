using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Fia_Clientes")]
public partial class FiaClientes
{
    [Key]
    [Column("Id_Cliente")]
    public int IdCliente { get; set; }

    [Column("Nombre")]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Column("Telefono")]
    [StringLength(20)]
    public string? Telefono { get; set; }

    [Column("Saldo_A_Favor")]
    public int SaldoAFavor { get; set; }

    [Column("Estado")]
    public bool Estado { get; set; } = true;

    [Column("Fecha_Registro", TypeName = "datetime")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [InverseProperty("IdClienteFiadoNavigation")]
    public virtual ICollection<VenBoletas> VenBoletas { get; set; } = new List<VenBoletas>();

    [InverseProperty("IdClienteNavigation")]
    public virtual ICollection<FiaAbonos> FiaAbonos { get; set; } = new List<FiaAbonos>();
}
