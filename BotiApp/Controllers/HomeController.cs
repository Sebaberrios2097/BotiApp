using BotiApp.Helpers;
using BotiApp.Models;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BotiApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IVentasRepository   _ventas;
        private readonly IProductosRepository _productos;

        public HomeController(IVentasRepository ventas, IProductosRepository productos)
        {
            _ventas    = ventas;
            _productos = productos;
        }

        // ── Dashboard ─────────────────────────────────────────────────────────
        // Carga KPIs diferenciados según el rol del usuario autenticado.
        public async Task<IActionResult> Index()
        {
            var hoy  = DateTime.Today;
            var anio = hoy.Year;
            var mes  = hoy.Month;

            var vm = new DashboardViewModel
            {
                NombreUsuario = ClaimHelper.GetNombreCompleto(User),
                TipoUsuario   = ClaimHelper.GetTipoUsuario(User)
            };

            if (ClaimHelper.EsAdmin(User))
            {
                var boletas   = (await _ventas.ObtenerBoletasDelMesAsync(anio, mes)).ToList();
                var productos = (await _productos.ObtenerTodosAsync()).ToList();

                vm.TotalBoletasMes           = boletas.Count;
                vm.TotalBoletasPagadasMes    = boletas.Count(b => b.IdEstadoBoleta == 3);
                vm.TotalBoletasAnuladasMes   = boletas.Count(b => b.IdEstadoBoleta == 2);
                vm.TotalBoletasPendientesMes = boletas.Count(b => b.IdEstadoBoleta == 1);
                vm.MontoTotalMes             = boletas.Where(b => b.IdEstadoBoleta == 3).Sum(b => (long)b.MontoTotal);
                vm.TotalProductosBajoStock   = productos.Count(p => p.Stock <= 5 && p.Estado);
                vm.UltimasBoletas            = boletas.Take(15);
            }
            else if (ClaimHelper.EsVendedor(User))
            {
                var idUsuario = ClaimHelper.GetIdUsuario(User);
                var boletas   = (await _ventas.ObtenerBoletasVendedorDelMesAsync(idUsuario, anio, mes)).ToList();

                vm.VendedorBoletasMes        = boletas.Count;
                vm.VendedorMontoMes          = boletas.Where(b => b.IdEstadoBoleta == 3).Sum(b => (long)b.MontoTotal);
                vm.VendedorBoletasPendientes = boletas.Count(b => b.IdEstadoBoleta == 1);
                vm.VendedorUltimasBoletas    = boletas.Take(10);
            }
            else if (ClaimHelper.EsCajero(User))
            {
                var idUsuario = ClaimHelper.GetIdUsuario(User);
                var boletas   = (await _ventas.ObtenerBoletasCajeroDelMesAsync(idUsuario, anio, mes)).ToList();

                vm.CajeroBoletasCobradas = boletas.Count(b => b.IdEstadoBoleta == 3);
                vm.CajeroBoletasAnuladas = boletas.Count(b => b.IdEstadoBoleta == 2);
                vm.CajeroMontoGestionado = boletas.Where(b => b.IdEstadoBoleta == 3).Sum(b => (long)b.MontoTotal);
                vm.CajeroUltimasBoletas  = boletas.Take(10);
            }

            return View(vm);
        }

        // ── Vistas estáticas ──────────────────────────────────────────────────
        public IActionResult Privacy()        => View();
        public IActionResult AccesoDenegado() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
