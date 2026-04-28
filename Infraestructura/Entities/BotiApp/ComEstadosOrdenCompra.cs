using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Com_Estados_Orden_Compra")]
public partial class ComEstadosOrdenCompra
{
    [Key]
    [Column("Id_Estado_Orden_Compra")]
    public int IdEstadoOrdenCompra { get; set; }

    [Column("Nombre_Estado_OC")]
    [StringLength(50)]
    public string NombreEstadoOc { get; set; } = null!;

    [InverseProperty("IdEstadoOrdenCompraNavigation")]
    public virtual ICollection<ComOrdenCompra> ComOrdenCompra { get; set; } = new List<ComOrdenCompra>();
}
