using BotiApp.Models;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotiApp.Areas.Productos.Controllers;

[Area("Productos")]
[Authorize(Policy = "SoloAdmin")]
public class OfertasController(IOfertasRepository ofertasRepository) : Controller
{
    // GET /Productos/Ofertas/Producto/5
    [HttpGet]
    public async Task<IActionResult> Producto(int id)
    {
        var producto = await ofertasRepository.ObtenerProductoAsync(id);
        if (producto is null) return NotFound();

        var vm = new OfertaProductoViewModel
        {
            Producto      = producto,
            OfertaActiva  = await ofertasRepository.ObtenerActivaPorProductoAsync(id),
            Historial     = await ofertasRepository.ObtenerHistorialPorProductoAsync(id),
            PromosActivas = await ofertasRepository.ObtenerPromosActivasPorProductoAsync(id),
        };

        return View(vm);
    }

    // POST /Productos/Ofertas/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(int idProducto, int precioOferta,
        DateTime fechaInicio, DateTime? fechaTermino)
    {
        if (precioOferta <= 0)
            return Json(new { ok = false, mensaje = "El precio de oferta debe ser mayor a 0." });

        if (fechaTermino.HasValue && fechaTermino.Value.Date < fechaInicio.Date)
            return Json(new { ok = false, mensaje = "La fecha de término no puede ser anterior a la de inicio." });

        // Desactivar oferta previa si existe
        var activa = await ofertasRepository.ObtenerActivaPorProductoAsync(idProducto);
        if (activa is not null)
            await ofertasRepository.DesactivarAsync(activa.IdOfertaProducto);

        await ofertasRepository.CrearAsync(new ProOfertaProducto
        {
            IdProducto         = idProducto,
            PrecioOferta       = precioOferta,
            FechaInicioOferta  = fechaInicio,
            FechaTerminoOferta = fechaTermino,
            Estado             = true,
        });

        return Json(new { ok = true, mensaje = "Oferta creada correctamente." });
    }

    // GET /Productos/Ofertas/ModalOfertas/5
    [HttpGet]
    public async Task<IActionResult> ModalOfertas(int id)
    {
        var producto = await ofertasRepository.ObtenerProductoAsync(id);
        if (producto is null) return NotFound();

        var vm = new OfertaProductoViewModel
        {
            Producto      = producto,
            OfertaActiva  = await ofertasRepository.ObtenerActivaPorProductoAsync(id),
            Historial     = await ofertasRepository.ObtenerHistorialPorProductoAsync(id),
            PromosActivas = await ofertasRepository.ObtenerPromosActivasPorProductoAsync(id),
        };

        return PartialView("_ModalOfertas", vm);
    }

    // POST /Productos/Ofertas/Desactivar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var ok = await ofertasRepository.DesactivarAsync(id);
        return ok
            ? Json(new { ok = true,  mensaje = "Oferta desactivada." })
            : Json(new { ok = false, mensaje = "No se encontró la oferta." });
    }

    // POST /Productos/Ofertas/Activar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var ok = await ofertasRepository.ActivarAsync(id);
        return ok
            ? Json(new { ok = true,  mensaje = "Oferta activada." })
            : Json(new { ok = false, mensaje = "No se pudo activar: ya existe una oferta activa para este producto." });
    }

    // POST /Productos/Ofertas/Modificar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Modificar(int id, int precioOferta,
        DateTime fechaInicio, DateTime? fechaTermino)
    {
        if (precioOferta <= 0)
            return Json(new { ok = false, mensaje = "El precio de oferta debe ser mayor a 0." });

        if (fechaTermino.HasValue && fechaTermino.Value.Date < fechaInicio.Date)
            return Json(new { ok = false, mensaje = "La fecha de término no puede ser anterior a la de inicio." });

        var ok = await ofertasRepository.ActualizarAsync(id, precioOferta, fechaInicio, fechaTermino);
        return ok
            ? Json(new { ok = true,  mensaje = "Oferta actualizada correctamente." })
            : Json(new { ok = false, mensaje = "No se encontró la oferta." });
    }
}
