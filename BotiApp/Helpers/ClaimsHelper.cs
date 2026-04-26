using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace BotiApp.Helpers
{
    public static class ClaimHelper
    {
        public static ClaimsPrincipal BuildPrincipal(
            string usuario, string nombreEmpleado,
            int rutEmpleado, string tipoUsuario, int idUsuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,    usuario),
                new Claim("NombreCompleto",   nombreEmpleado),
                new Claim("Rut",              rutEmpleado.ToString()),
                new Claim("TipoUsuario",      tipoUsuario),
                new Claim("IdUsuario",        idUsuario.ToString()),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        public static AuthenticationProperties BuildAuthProperties(bool recordar) =>
            new()
            {
                IsPersistent = recordar,
                ExpiresUtc = recordar
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

        public static string GetUsuario(ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        public static string GetNombreCompleto(ClaimsPrincipal user)
            => user.FindFirstValue("NombreCompleto") ?? string.Empty;

        public static int GetRut(ClaimsPrincipal user)
        {
            var rutString = user.FindFirstValue("Rut");
            return int.TryParse(rutString, out int rut) ? rut : 0;
        }

        public static int GetIdUsuario(ClaimsPrincipal user)
        {
            var val = user.FindFirstValue("IdUsuario");
            return int.TryParse(val, out int id) ? id : 0;
        }

        public static string GetTipoUsuario(ClaimsPrincipal user)
            => user.FindFirstValue("TipoUsuario") ?? string.Empty;

        public static bool EsAdmin(ClaimsPrincipal user)
            => user.FindFirstValue("TipoUsuario") == "Administrador";
    }
}