using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Ven_Estados_Boletas")]
public partial class VenEstadosBoletas
{
    [Key]
    [Column("Id_Estado_Boleta")]
    public int IdEstadoBoleta { get; set; }

    [Column("Nombre_Estado_Boleta")]
    [StringLength(50)]
    public string NombreEstadoBoleta { get; set; } = null!;

    [InverseProperty("IdEstadoBoletaNavigation")]
    public virtual ICollection<VenBoletas> VenBoletas { get; set; } = new List<VenBoletas>();
}
