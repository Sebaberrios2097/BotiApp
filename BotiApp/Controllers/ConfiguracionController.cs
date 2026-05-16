using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotiApp.Controllers
{
    [Authorize(Policy = "SoloAdmin")]
    public class ConfiguracionController : Controller
    {
        private readonly IMantenedoresRepository _man;

        public ConfiguracionController(IMantenedoresRepository man)
        {
            _man = man;
        }

        // GET /Configuracion/Index
        public async Task<IActionResult> Index()
        {
            ViewBag.TiposProducto = await _man.GetTiposProductoAsync();
            ViewBag.Marcas        = await _man.GetMarcasAsync();
            ViewBag.MetodosPago   = await _man.GetMetodosPagoAsync();
            return View();
        }

        /* ── Tipos de Producto ───────────────────────────────────────────── */

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearTipoProducto([FromBody] NombreRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Json(new { ok = false, mensaje = "El nombre es obligatorio." });

            var entidad = await _man.CreateTipoProductoAsync(req.Nombre);
            return Json(new { ok = true, id = entidad.IdTipoProducto, nombre = entidad.NombreTipoProducto });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarTipoProducto([FromBody] IdNombreRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Json(new { ok = false, mensaje = "El nombre es obligatorio." });

            var entidad = await _man.UpdateTipoProductoAsync(req.Id, req.Nombre);
            if (entidad is null) return Json(new { ok = false, mensaje = "Registro no encontrado." });
            return Json(new { ok = true, id = entidad.IdTipoProducto, nombre = entidad.NombreTipoProducto });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarTipoProducto([FromBody] IdRequest req)
        {
            var ok = await _man.DeleteTipoProductoAsync(req.Id);
            return Json(new { ok, mensaje = ok ? "Eliminado correctamente." : "Registro no encontrado o tiene productos asociados." });
        }

        /* ── Marcas ──────────────────────────────────────────────────────── */

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearMarca([FromBody] NombreRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Json(new { ok = false, mensaje = "El nombre es obligatorio." });

            var entidad = await _man.CreateMarcaAsync(req.Nombre);
            return Json(new { ok = true, id = entidad.IdMarca, nombre = entidad.NombreMarca, estado = entidad.Estado });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarMarca([FromBody] IdNombreRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Json(new { ok = false, mensaje = "El nombre es obligatorio." });

            var entidad = await _man.UpdateMarcaAsync(req.Id, req.Nombre);
            if (entidad is null) return Json(new { ok = false, mensaje = "Registro no encontrado." });
            return Json(new { ok = true, id = entidad.IdMarca, nombre = entidad.NombreMarca, estado = entidad.Estado });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMarca([FromBody] IdRequest req)
        {
            var estado = await _man.ToggleMarcaAsync(req.Id);
            if (estado is null) return Json(new { ok = false, mensaje = "Registro no encontrado." });
            return Json(new { ok = true, estado });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarMarca([FromBody] IdRequest req)
        {
            var ok = await _man.DeleteMarcaAsync(req.Id);
            return Json(new { ok, mensaje = ok ? "Eliminado correctamente." : "Registro no encontrado o tiene productos asociados." });
        }

        /* ── Métodos de Pago ─────────────────────────────────────────────── */

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearMetodoPago([FromBody] NombreRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Json(new { ok = false, mensaje = "El nombre es obligatorio." });

            var entidad = await _man.CreateMetodoPagoAsync(req.Nombre);
            return Json(new { ok = true, id = entidad.IdMetodoPago, nombre = entidad.NombreMetodoPago });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarMetodoPago([FromBody] IdNombreRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return Json(new { ok = false, mensaje = "El nombre es obligatorio." });

            var entidad = await _man.UpdateMetodoPagoAsync(req.Id, req.Nombre);
            if (entidad is null) return Json(new { ok = false, mensaje = "Registro no encontrado." });
            return Json(new { ok = true, id = entidad.IdMetodoPago, nombre = entidad.NombreMetodoPago });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarMetodoPago([FromBody] IdRequest req)
        {
            var ok = await _man.DeleteMetodoPagoAsync(req.Id);
            return Json(new { ok, mensaje = ok ? "Eliminado correctamente." : "Registro no encontrado o tiene ventas asociadas." });
        }

        /* ── Records ──────────────────────────────────────────────────────── */
        public record NombreRequest(string Nombre);
        public record IdNombreRequest(int Id, string Nombre);
        public record IdRequest(int Id);
    }
}
