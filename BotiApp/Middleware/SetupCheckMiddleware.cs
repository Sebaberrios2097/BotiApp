using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BotiApp.Middleware
{
    public class SetupCheckMiddleware
    {
        private readonly RequestDelegate _next;

        // Prefijos de ruta que deben omitirse para evitar bucles de redirección
        private static readonly string[] _rutasExcluidas =
        [
            "/setup",
            "/login",
            "/css",
            "/js",
            "/lib",
            "/favicon"
        ];

        public SetupCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            bool esExcluida = _rutasExcluidas.Any(r =>
                path.StartsWith(r, StringComparison.OrdinalIgnoreCase));

            if (!esExcluida)
            {
                var setup = context.RequestServices.GetRequiredService<ISetupRepository>();

                if (await setup.RequiereSetupAsync())
                {
                    context.Response.Redirect("/Setup/Index");
                    return;
                }
            }

            await _next(context);
        }
    }
}
