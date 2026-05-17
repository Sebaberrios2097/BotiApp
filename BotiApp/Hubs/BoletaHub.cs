using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BotiApp.Hubs;

/// <summary>
/// Hub que notifica a los cajeros cuando un vendedor genera una nueva boleta.
/// Solo usuarios autenticados pueden conectarse.
/// </summary>
[Authorize]
public class BoletaHub : Hub { }
