using Infraestructura.Repositories.BotiApp.Interfaces;

namespace BotiApp.Services.Sii;

public class SiiBoletaService(ISiiBoletaSimuladaRepository siiBoletaSimuladaRepository) : ISiiBoletaService
{
    public async Task<SiiBoletaServiceResult> EmitirBoletaAfectaAsync(int idBoleta, bool forzarReintento = false)
    {
        var result = await siiBoletaSimuladaRepository.EmitirBoletaAfectaAsync(idBoleta, forzarReintento);

        return new SiiBoletaServiceResult(
            result.Ok,
            result.EstadoSii,
            result.Mensaje,
            result.Folio,
            result.TrackId,
            result.Intentos
        );
    }
}
