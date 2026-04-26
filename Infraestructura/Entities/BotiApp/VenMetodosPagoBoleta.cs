using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Ven_Metodos_Pago_Boleta")]
public partial class VenMetodosPagoBoleta
{
    [Key]
    [Column("Id_Metodo_Pago_Boleta")]
    public int IdMetodoPagoBoleta { get; set; }

    [Column("Id_Metodo_Pago")]
    public int IdMetodoPago { get; set; }

    [Column("Id_Boleta")]
    public int IdBoleta { get; set; }

    public int Monto { get; set; }

    [ForeignKey("IdBoleta")]
    [InverseProperty("VenMetodosPagoBoleta")]
    public virtual VenBoletas IdBoletaNavigation { get; set; } = null!;

    [ForeignKey("IdMetodoPago")]
    [InverseProperty("VenMetodosPagoBoleta")]
    public virtual VenMetodosPago IdMetodoPagoNavigation { get; set; } = null!;
}
