using System.Xml.Linq;
using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class SiiBoletaSimuladaRepository(BotiAppContext context) : ISiiBoletaSimuladaRepository
{
    private const int TipoDteBoletaAfecta = 39;
    private const decimal Iva = 0.19m;
    private const int FolioInicialDefault = 1;
    private const int FolioFinalDefault = 99999999;

    public async Task<SiiSimulacionBoletaResult> EmitirBoletaAfectaAsync(int idBoleta, bool forzarReintento = false)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var boleta = await context.VenBoletas
            .Include(b => b.VenBoletaDetalle)
            .FirstOrDefaultAsync(b => b.IdBoleta == idBoleta);

        if (boleta == null)
            return new(false, "NO_EMITIDO", $"No existe la boleta N° {idBoleta}.", null, null, 0);

        if (boleta.IdEstadoBoleta != 3)
        {
            var estadoActual = boleta.EstadoSii ?? "NO_EMITIDO";
            return new(false, estadoActual, "La boleta debe estar pagada para emitir DTE simulado.", boleta.FolioSii, boleta.TrackIdSii, boleta.IntentosEnvioSii ?? 0);
        }

        if (!forzarReintento && string.Equals(boleta.EstadoSii, "ACEPTADO", StringComparison.OrdinalIgnoreCase))
            return new(true, boleta.EstadoSii!, $"SII simulado: boleta ya aceptada con folio {boleta.FolioSii}.", boleta.FolioSii, boleta.TrackIdSii, boleta.IntentosEnvioSii ?? 0);

        var secuencia = await context.VenSiiFolios.FirstOrDefaultAsync(s => s.TipoDte == TipoDteBoletaAfecta);
        if (secuencia == null)
        {
            secuencia = new VenSiiFolios
            {
                TipoDte = TipoDteBoletaAfecta,
                FolioInicial = FolioInicialDefault,
                FolioFinal = FolioFinalDefault,
                FolioActual = FolioInicialDefault - 1,
                ActualizadoEn = DateTime.Now
            };
            context.VenSiiFolios.Add(secuencia);
            await context.SaveChangesAsync();
        }

        if (boleta.FolioSii is null or <= 0)
        {
            if (secuencia.FolioActual >= secuencia.FolioFinal)
            {
                boleta.EstadoSii = "ERROR_ENVIO";
                boleta.MensajeSii = "SII simulado: secuencia de folios agotada.";
                boleta.FechaEnvioSii = DateTime.Now;
                boleta.IntentosEnvioSii = (boleta.IntentosEnvioSii ?? 0) + 1;

                await context.SaveChangesAsync();
                await tx.CommitAsync();

                return new(false, boleta.EstadoSii, boleta.MensajeSii, null, boleta.TrackIdSii, boleta.IntentosEnvioSii.Value);
            }

            secuencia.FolioActual += 1;
            secuencia.ActualizadoEn = DateTime.Now;
            boleta.FolioSii = secuencia.FolioActual;
        }

        var intento = (boleta.IntentosEnvioSii ?? 0) + 1;
        var fechaEnvio = DateTime.Now;
        var trackId = $"SIM-{fechaEnvio:yyyyMMddHHmmss}-{boleta.IdBoleta}-{intento}";
        var (neto, iva, exento) = CalcularMontos(boleta.MontoTotal);
        var estado = ResolverEstado(boleta.IdBoleta, intento, forzarReintento);
        var mensaje = CrearMensaje(estado, boleta.FolioSii!.Value);

        boleta.TipoDteSii = TipoDteBoletaAfecta;
        boleta.EstadoSii = estado;
        boleta.TrackIdSii = trackId;
        boleta.FechaEnvioSii = fechaEnvio;
        boleta.MontoNetoSii = neto;
        boleta.MontoIvaSii = iva;
        boleta.MontoExentoSii = exento;
        boleta.IntentosEnvioSii = intento;
        boleta.MensajeSii = mensaje;
        boleta.XmlDteSii = GenerarXmlDte(boleta, neto, iva, exento, trackId);

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return new(estado == "ACEPTADO", estado, mensaje, boleta.FolioSii, trackId, intento);
    }

    private static (int Neto, int Iva, int Exento) CalcularMontos(int total)
    {
        if (total <= 0)
            return (0, 0, 0);

        var neto = (int)Math.Round(total / (1 + Iva), MidpointRounding.AwayFromZero);
        var iva = total - neto;
        return (neto, iva, 0);
    }

    private static string ResolverEstado(int idBoleta, int intento, bool forzarReintento)
    {
        if (forzarReintento)
            return "ACEPTADO";

        var bucket = Math.Abs(((idBoleta * 31) + (intento * 17)) % 100);

        if (bucket < 10)
            return "ERROR_ENVIO";
        if (bucket < 18)
            return "RECHAZADO";
        if (bucket < 25)
            return "PENDIENTE_REINTENTO";

        return "ACEPTADO";
    }

    private static string CrearMensaje(string estado, int folio)
        => estado switch
        {
            "ACEPTADO" => $"SII simulado: boleta afecta 39 aceptada con folio {folio}.",
            "RECHAZADO" => $"SII simulado: boleta afecta 39 rechazada para el folio {folio} (validacion simulada).",
            "PENDIENTE_REINTENTO" => $"SII simulado: boleta afecta 39 pendiente de reintento para el folio {folio}.",
            _ => $"SII simulado: error transitorio de envio para el folio {folio}."
        };

    private static string GenerarXmlDte(VenBoletas boleta, int neto, int iva, int exento, string trackId)
    {
        var detalles = boleta.VenBoletaDetalle
            .OrderBy(d => d.IdBoletaDetalle)
            .Select((d, index) =>
                new XElement("Detalle",
                    new XElement("NroLinDet", index + 1),
                    new XElement("NmbItem", $"Producto {d.IdProducto}"),
                    new XElement("QtyItem", d.Cantidad),
                    new XElement("PrcItem", d.PrecioUnitario),
                    new XElement("MontoItem", d.Subtotal)
                ));

        var doc = new XDocument(
            new XElement("DteSimulado",
                new XAttribute("version", "1.0"),
                new XElement("Documento",
                    new XElement("Encabezado",
                        new XElement("IdBoletaInterna", boleta.IdBoleta),
                        new XElement("TipoDte", TipoDteBoletaAfecta),
                        new XElement("Folio", boleta.FolioSii ?? 0),
                        new XElement("FechaEmision", (boleta.FechaPago ?? boleta.FechaEmision ?? DateTime.Now).ToString("yyyy-MM-dd")),
                        new XElement("TrackId", trackId),
                        new XElement("Totales",
                            new XElement("MntNeto", neto),
                            new XElement("IVA", iva),
                            new XElement("MntExe", exento),
                            new XElement("MntTotal", boleta.MontoTotal)
                        )
                    ),
                    new XElement("DetalleItems", detalles)
                )
            )
        );

        return doc.ToString(SaveOptions.DisableFormatting);
    }
}
