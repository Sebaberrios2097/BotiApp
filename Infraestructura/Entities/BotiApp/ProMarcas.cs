using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Marcas")]
public partial class ProMarcas
{
    [Key]
    [Column("Id_Marca")]
    public int IdMarca { get; set; }

    [Column("Nombre_Marca")]
    [StringLength(100)]
    public string NombreMarca { get; set; } = null!;

    public bool Estado { get; set; }

    [InverseProperty("IdMarcaNavigation")]
    public virtual ICollection<ProProductos> ProProductos { get; set; } = new List<ProProductos>();
}
