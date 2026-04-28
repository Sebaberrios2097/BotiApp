using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Ven_Metodos_Pago")]
public partial class VenMetodosPago
{
    [Key]
    [Column("Id_Metodo_Pago")]
    public int IdMetodoPago { get; set; }

    [Column("Nombre_Metodo_Pago")]
    [StringLength(50)]
    public string NombreMetodoPago { get; set; } = null!;

    [InverseProperty("IdMetodoPagoNavigation")]
    public virtual ICollection<VenMetodosPagoBoleta> VenMetodosPagoBoleta { get; set; } = new List<VenMetodosPagoBoleta>();
}
