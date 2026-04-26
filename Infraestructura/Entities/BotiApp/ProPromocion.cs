using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Promocion")]
public partial class ProPromocion
{
    [Key]
    [Column("Id_Promocion")]
    public int IdPromocion { get; set; }

    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(150)]
    public string? Descripcion { get; set; }

    [Column("Precio_Promocion")]
    public int PrecioPromocion { get; set; }

    [Column("Fecha_Inicio", TypeName = "datetime")]
    public DateTime FechaInicio { get; set; }

    [Column("Fecha_Fin", TypeName = "datetime")]
    public DateTime? FechaFin { get; set; }

    public bool Estado { get; set; }

    [InverseProperty("IdPromocionNavigation")]
    public virtual ICollection<ProPromocionDetalle> ProPromocionDetalle { get; set; } = new List<ProPromocionDetalle>();

    [InverseProperty("IdPromocionNavigation")]
    public virtual ICollection<ProPromocionGrupo> ProPromocionGrupo { get; set; } = new List<ProPromocionGrupo>();
}