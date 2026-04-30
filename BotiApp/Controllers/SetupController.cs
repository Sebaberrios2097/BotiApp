using BotiApp.Models;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Infraestructura.Repositories.Sp.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BotiApp.Controllers
{
    public class SetupController : Controller
    {
        private readonly ISetupRepository _setup;
        private readonly IEmpEmpleadoRepository _empleado;
        private readonly IBotiAppStoredProcedures _sp;

        public SetupController(
            ISetupRepository setup,
            IEmpEmpleadoRepository empleado,
            IBotiAppStoredProcedures sp)
        {
            _setup = setup;
            _empleado = empleado;
            _sp = sp;
        }

        // GET /Setup/Index
        public async Task<IActionResult> Index()
        {
            // Si ya existe un administrador, no hay nada que configurar
            if (!await _setup.RequiereSetupAsync())
                return RedirectToAction("Login", "Login");

            return View(new SetupViewModel());
        }

        // POST /Setup/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SetupViewModel model)
        {
            // Doble verificación: si ya está configurado, no permitir re-ejecución
            if (!await _setup.RequiereSetupAsync())
                return RedirectToAction("Login", "Login");

            if (!ModelState.IsValid)
                return View(model);

            // 1. Asegurar que existan los tipos de usuario
            int idTipoAdmin = await _setup.EnsureTiposUsuarioAsync();

            // 2. Crear el empleado
            var empleado = new EmpEmpleado
            {
                NombresEmpleado = model.NombresEmpleado.Trim(),
                Apellido1       = model.Apellido1.Trim(),
                Apellido2       = string.IsNullOrWhiteSpace(model.Apellido2) ? null : model.Apellido2.Trim(),
                Rut             = model.Rut,
                Fono            = string.IsNullOrWhiteSpace(model.Fono)   ? null : model.Fono.Trim(),
                Correo          = string.IsNullOrWhiteSpace(model.Correo) ? null : model.Correo.Trim(),
                FechaIngreso    = DateTime.Now
            };

            var creado = await _empleado.Create(empleado);

            // 3. Crear el usuario administrador mediante SP (la clave se genera a partir del RUT)
            await _sp.SpEmpCreaUsuarioEmpleado(creado.Rut, idTipoAdmin);

            TempData["SetupExito"] = $"Administrador creado exitosamente. " +
                $"Su usuario es su nombre y su contraseña inicial es su RUT sin dígito verificador.";

            return RedirectToAction("Login", "Login");
        }
    }
}
