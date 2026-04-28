using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotiApp.Areas.Ventas.Controllers;

[Area("Ventas")]
[Authorize(Policy = "TodosRoles")]
public class CatalogoController(
    IProductosRepository productosRepo,
    IPromocionesRepository promoRepo) : Controller
{
    // ── GET /Ventas/Catalogo/Productos ────────────────────────────────────────
    public async Task<IActionResult> Productos()
    {
        var productos = await productosRepo.ObtenerTodosAsync();
        return View(productos);
    }

    // ── GET /Ventas/Catalogo/Promociones ──────────────────────────────────────
    public async Task<IActionResult> Promociones()
    {
        var promos = await promoRepo.ObtenerTodasAsync();
        return View(promos);
    }

    // ── GET: imagen producto ─────────────────────────────────────────────────
    public async Task<IActionResult> Imagen(int id)
    {
        var p = await productosRepo.ObtenerPorIdAsync(id);
        if (p?.Imagen is null) return NotFound();
        return File(p.Imagen, "image/jpeg");
    }
}
