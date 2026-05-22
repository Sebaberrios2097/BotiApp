namespace Infraestructura.Repositories.BotiApp.Interfaces;

public interface ISiiBoletaSimuladaRepository
{
    Task<SiiSimulacionBoletaResult> EmitirBoletaAfectaAsync(int idBoleta, bool forzarReintento = false);
}

public sealed record SiiSimulacionBoletaResult(
    bool Ok,
    string EstadoSii,
    string Mensaje,
    int? Folio,
    string? TrackId,
    int Intentos
);
