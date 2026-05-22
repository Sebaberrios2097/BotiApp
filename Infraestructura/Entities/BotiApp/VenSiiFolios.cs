using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Ven_Sii_Folios")]
public partial class VenSiiFolios
{
    [Key]
    [Column("Id_Folio_Secuencia")]
    public int IdFolioSecuencia { get; set; }

    [Column("Tipo_Dte")]
    public int TipoDte { get; set; }

    [Column("Folio_Actual")]
    public int FolioActual { get; set; }

    [Column("Folio_Inicial")]
    public int FolioInicial { get; set; }

    [Column("Folio_Final")]
    public int FolioFinal { get; set; }

    [Column("Actualizado_En", TypeName = "datetime")]
    public DateTime ActualizadoEn { get; set; }
}
