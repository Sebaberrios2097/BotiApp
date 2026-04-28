using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Pro_Proveedores_Dias_Entrega")]
public partial class ProProveedoresDiasEntrega
{
    [Key]
    [Column("Id_Dia_Entrega")]
    public int IdDiaEntrega { get; set; }

    [Column("Id_Proveedor")]
    public int IdProveedor { get; set; }

    [Column("Dia_Semana")]
    public int DiaSemana { get; set; }

    [StringLength(300)]
    public string? Observacion { get; set; }

    [ForeignKey("IdProveedor")]
    [InverseProperty("ProProveedoresDiasEntrega")]
    public virtual ProProveedores IdProveedorNavigation { get; set; } = null!;
}
