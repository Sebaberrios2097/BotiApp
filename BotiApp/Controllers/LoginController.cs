using BotiApp.Helpers;
using BotiApp.Models;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Infraestructura.Repositories.Sp.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace BotiApp.Controllers
{
    public class LoginController : Controller
    {
        private readonly IBotiAppStoredProcedures _sp;
        private readonly IEmpEmpleadoRepository _empleado;
        private readonly IEmpUsuariosRepository _usuario;

        public LoginController(IBotiAppStoredProcedures sp,
                               IEmpEmpleadoRepository empleado,
                               IEmpUsuariosRepository usuario)
        {
            _sp = sp;
            _empleado = empleado;
            _usuario = usuario;
        }

        // GET: /Login/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Login/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var validaAcceso = await _sp.SpEmpValidaAccesoUsuario(model.Usuario, model.Clave);

            if (validaAcceso == 1)
            {
                // Obtener datos del usuario (viene con datos del empleado)
                var usuario = await _usuario.GetByNombreUsuario(model.Usuario);
                var nombreEmpleado = $"{usuario.IdEmpleadoNavigation.NombresEmpleado} {usuario.IdEmpleadoNavigation.Apellido1} {usuario.IdEmpleadoNavigation.Apellido2}";
                var rutEmpleado = usuario.IdEmpleadoNavigation.Rut;
                var tipoUsuario = usuario.IdTipoUsuarioNavigation.NombreTipoUsuario;

                var principal = ClaimHelper.BuildPrincipal(
                    model.Usuario, nombreEmpleado, rutEmpleado, tipoUsuario, usuario.IdUsuario);
                var authProperties = ClaimHelper.BuildAuthProperties(model.Recordar);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                return RedirectToAction("Index", "Home");
            }
            else if (validaAcceso == 2)
            {
                ModelState.AddModelError(string.Empty, "Cuenta de usuario inhabilitada, por favor contactar con el administrador.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(model);
        }

        // GET/POST: /Login/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // GET: /Login/CambiarContrasena
        public IActionResult CambiarContrasena()
        {
            return View(new CambiarContrasenaViewModel());
        }

        // POST: /Login/CambiarContrasena
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(CambiarContrasenaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int resultado = await _sp.SpEmpCambiaContrasena(model.Usuario, model.ClaveActual, model.ClaveNueva);

            if (resultado == 1)
            {
                TempData["CambioExito"] = "Contraseña actualizada correctamente. Inicia sesión con tu nueva contraseña.";
                return RedirectToAction("Login");
            }
            else if (resultado == -1)
            {
                ModelState.AddModelError(string.Empty, "Usuario no encontrado.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "La contraseña actual es incorrecta.");
            }

            return View(model);
        }
    }
}
