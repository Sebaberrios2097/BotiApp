namespace BotiApp.Services.Sii;

public interface ISiiBoletaService
{
    Task<SiiBoletaServiceResult> EmitirBoletaAfectaAsync(int idBoleta, bool forzarReintento = false);
}

public sealed record SiiBoletaServiceResult(
    bool Ok,
    string EstadoSii,
    string Mensaje,
    int? Folio,
    string? TrackId,
    int Intentos
);
