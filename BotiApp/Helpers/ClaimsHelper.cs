using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace BotiApp.Helpers
{
    public static class ClaimHelper
    {
        /// <summary>
        /// Construye el ClaimsPrincipal con los datos del usuario autenticado.
        /// </summary>
        public static ClaimsPrincipal BuildPrincipal(string usuario, string nombreEmpleado, int rutEmpleado, string tipoUsuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario),
                new Claim("NombreCompleto", nombreEmpleado),
                new Claim("Rut", rutEmpleado.ToString()),
                new Claim("TipoUsuario", tipoUsuario),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Construye las propiedades de autenticación (persistencia y expiración).
        /// </summary>
        public static AuthenticationProperties BuildAuthProperties(bool recordar)
        {
            return new AuthenticationProperties
            {
                IsPersistent = recordar,
                ExpiresUtc = recordar
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };
        }

        /// <summary>
        /// Obtiene el nombre del usuario desde el ClaimsPrincipal actual.
        /// </summary>
        public static string GetUsuario(ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        /// <summary>
        /// Obtiene el nombre completo del usuario
        /// </summary>
        public static string GetNombreCompleto(ClaimsPrincipal user)
            => user.FindFirstValue("NombreCompleto") ?? string.Empty;

        /// <summary>
        /// Obtiene el rut del usuario
        /// </summary>
        public static int GetRut(ClaimsPrincipal user)
        {
            var rutString = user.FindFirstValue("Rut");
            return int.TryParse(rutString, out int rut) ? rut : 0;
        }

        /// <summary>
        /// Obtiene el tipo de usuario
        /// </summary>
        public static string GetTipoUsuario(ClaimsPrincipal user)
            => user.FindFirstValue("TipoUsuario") ?? string.Empty;

        /// <summary>
        /// Obtiene el rut del usuario
        /// </summary>
        public static bool EsAdmin(ClaimsPrincipal user)
        {
            var tipoUsuario = user.FindFirstValue("TipoUsuario");
            return tipoUsuario == "Administrador";
        }
    }
}