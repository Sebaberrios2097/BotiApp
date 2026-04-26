using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Ven_Boletas")]
public partial class VenBoletas
{
    [Key]
    [Column("Id_Boleta")]
    public int IdBoleta { get; set; }

    [Column("Id_Usuario")]
    public int IdUsuario { get; set; }

    [Column("Id_Estado_Boleta")]
    public int IdEstadoBoleta { get; set; }

    [Column("Fecha_Emision", TypeName = "datetime")]
    public DateTime? FechaEmision { get; set; }

    [Column("Monto_Total")]
    public int MontoTotal { get; set; }

    [Column("Fecha_Pago", TypeName = "datetime")]
    public DateTime? FechaPago { get; set; }

    [ForeignKey("IdEstadoBoleta")]
    [InverseProperty("VenBoletas")]
    public virtual VenEstadosBoletas IdEstadoBoletaNavigation { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    [InverseProperty("VenBoletas")]
    public virtual EmpUsuario IdUsuarioNavigation { get; set; } = null!;

    [InverseProperty("IdBoletaNavigation")]
    public virtual ICollection<VenBoletaDetalle> VenBoletaDetalle { get; set; } = new List<VenBoletaDetalle>();

    [InverseProperty("IdBoletaNavigation")]
    public virtual ICollection<VenMetodosPagoBoleta> VenMetodosPagoBoleta { get; set; } = new List<VenMetodosPagoBoleta>();
}
