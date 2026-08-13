using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace BotiApp.Helpers
{
    public static class ClaimHelper
    {
        // ── Construcción de principal ─────────────────────────────────────────
        // Crea el ClaimsPrincipal a partir de los datos del usuario autenticado.
        // El tipo de usuario puede ser un rol combinado (ej. "Cajero/Vendedor"),
        // en cuyo caso se emite una claim "TipoUsuario" por cada rol individual
        // para que las policies (RequireClaim) y los chequeos de rol le den al
        // usuario el acceso de ambos roles. "TipoUsuarioLabel" conserva el texto
        // original tal como está en la base de datos, solo para mostrarlo en pantalla.
        public static ClaimsPrincipal BuildPrincipal(
            string usuario, string nombreEmpleado,
            int rutEmpleado, string tipoUsuario, int idUsuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario),
                new Claim("NombreCompleto",  nombreEmpleado),
                new Claim("Rut",             rutEmpleado.ToString()),
                new Claim("TipoUsuarioLabel", tipoUsuario),
                new Claim("IdUsuario",       idUsuario.ToString()),
            };

            foreach (var rol in tipoUsuario.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                claims.Add(new Claim("TipoUsuario", rol));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        // Configura la cookie: persistente 30 días si "recordar", 8 horas de lo contrario.
        public static AuthenticationProperties BuildAuthProperties(bool recordar) =>
            new()
            {
                IsPersistent = recordar,
                ExpiresUtc   = recordar
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

        // ── Lectura de claims ─────────────────────────────────────────────────
        public static string GetUsuario(ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        public static string GetNombreCompleto(ClaimsPrincipal user)
            => user.FindFirstValue("NombreCompleto") ?? string.Empty;

        public static int GetIdUsuario(ClaimsPrincipal user)
        {
            var val = user.FindFirstValue("IdUsuario");
            return int.TryParse(val, out int id) ? id : 0;
        }

        // Texto original (puede ser un rol combinado, ej. "Cajero/Vendedor") para mostrar en pantalla.
        public static string GetTipoUsuario(ClaimsPrincipal user)
            => user.FindFirstValue("TipoUsuarioLabel") ?? user.FindFirstValue("TipoUsuario") ?? string.Empty;

        // ── Verificación de rol ─────────────────────────────────────────────
        // Un usuario puede tener más de una claim "TipoUsuario" (rol combinado),
        // por lo que se chequea si CUALQUIERA de ellas coincide, no solo la primera.
        public static bool EsAdmin(ClaimsPrincipal user)
            => user.HasClaim("TipoUsuario", "Administrador");

        public static bool EsVendedor(ClaimsPrincipal user)
            => user.HasClaim("TipoUsuario", "Vendedor");

        public static bool EsCajero(ClaimsPrincipal user)
            => user.HasClaim("TipoUsuario", "Cajero");
    }
}
