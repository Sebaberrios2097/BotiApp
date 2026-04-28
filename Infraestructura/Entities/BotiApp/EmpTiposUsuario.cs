using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Emp_Tipos_Usuario")]
public partial class EmpTiposUsuario
{
    [Key]
    [Column("Id_Tipo_Usuario")]
    public int IdTipoUsuario { get; set; }

    [Column("Nombre_Tipo_Usuario")]
    [StringLength(50)]
    public string NombreTipoUsuario { get; set; } = null!;

    [InverseProperty("IdTipoUsuarioNavigation")]
    public virtual ICollection<EmpUsuario> EmpUsuario { get; set; } = new List<EmpUsuario>();
}
