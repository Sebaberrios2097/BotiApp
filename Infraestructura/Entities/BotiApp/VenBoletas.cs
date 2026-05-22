using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Entities.BotiApp;

[Table("Ven_Boletas")]
public partial class VenBoletas
{
    [Key]
    [Column("Id_Boleta")]
    public int IdBoleta { get; set; }

    [Column("Id_Vendedor")]
    public int IdVendedor { get; set; }

    [Column("Id_Cajero")]
    public int? IdCajero { get; set; }

    [Column("Id_Estado_Boleta")]
    public int IdEstadoBoleta { get; set; }

    [Column("Fecha_Emision", TypeName = "datetime")]
    public DateTime? FechaEmision { get; set; }

    [Column("Monto_Total")]
    public int MontoTotal { get; set; }

    [Column("Fecha_Pago", TypeName = "datetime")]
    public DateTime? FechaPago { get; set; }

    [Column("Tipo_Dte_Sii")]
    public int? TipoDteSii { get; set; }

    [Column("Folio_Sii")]
    public int? FolioSii { get; set; }

    [Column("Estado_Sii")]
    [StringLength(30)]
    public string? EstadoSii { get; set; }

    [Column("TrackId_Sii")]
    [StringLength(60)]
    public string? TrackIdSii { get; set; }

    [Column("Fecha_Envio_Sii", TypeName = "datetime")]
    public DateTime? FechaEnvioSii { get; set; }

    [Column("Monto_Neto_Sii")]
    public int? MontoNetoSii { get; set; }

    [Column("Monto_Iva_Sii")]
    public int? MontoIvaSii { get; set; }

    [Column("Monto_Exento_Sii")]
    public int? MontoExentoSii { get; set; }

    [Column("Xml_Dte_Sii")]
    public string? XmlDteSii { get; set; }

    [Column("Mensaje_Sii")]
    [StringLength(300)]
    public string? MensajeSii { get; set; }

    [Column("Intentos_Envio_Sii")]
    public int? IntentosEnvioSii { get; set; }

    public string? Observaciones { get; set; }

    [ForeignKey("IdEstadoBoleta")]
    [InverseProperty("VenBoletas")]
    public virtual VenEstadosBoletas IdEstadoBoletaNavigation { get; set; } = null!;

    [ForeignKey("IdVendedor")]
    [InverseProperty("VenBoletasVendedor")]
    public virtual EmpUsuario IdVendedorNavigation { get; set; } = null!;

    [Column("Id_Cliente_Fiado")]
    public int? IdClienteFiado { get; set; }

    [ForeignKey("IdCajero")]
    [InverseProperty("VenBoletasCajero")]
    public virtual EmpUsuario? IdCajeroNavigation { get; set; }

    [ForeignKey("IdClienteFiado")]
    [InverseProperty("VenBoletas")]
    public virtual FiaClientes? IdClienteFiadoNavigation { get; set; }

    [InverseProperty("IdBoletaNavigation")]
    public virtual ICollection<VenBoletaDetalle> VenBoletaDetalle { get; set; } = new List<VenBoletaDetalle>();

    [InverseProperty("IdBoletaNavigation")]
    public virtual ICollection<VenMetodosPagoBoleta> VenMetodosPagoBoleta { get; set; } = new List<VenMetodosPagoBoleta>();
}
