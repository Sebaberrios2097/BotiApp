using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Promocion_Grupo")]
public partial class ProPromocionGrupo
{
    [Key]
    [Column("Id_Grupo")]
    public int IdGrupo { get; set; }

    [Column("Id_Promocion")]
    public int IdPromocion { get; set; }

    [StringLength(100)]
    public string Descripcion { get; set; } = null!;

    [Column("Es_Excluyente")]
    public bool EsExcluyente { get; set; } = true;

    [ForeignKey("IdPromocion")]
    [InverseProperty("ProPromocionGrupo")]
    public virtual ProPromocion IdPromocionNavigation { get; set; } = null!;

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<ProPromocionDetalle> ProPromocionDetalle { get; set; } = new List<ProPromocionDetalle>();
}