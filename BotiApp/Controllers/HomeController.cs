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
        public async Task<IActionResult> Index(int? mes, int? anio)
        {
            var hoy = DateTime.Today;

            // Períodos con al menos una boleta emitida (desc)
            var periodos = (await _ventas.ObtenerPeriodosConMovimientoAsync()).ToList();

            // Período seleccionado: parámetro de URL > más reciente con movimiento > mes actual
            var (anioSel, mesSel) = (mes.HasValue && anio.HasValue)
                ? (anio.Value, mes.Value)
                : periodos.Count > 0 ? periodos[0] : (hoy.Year, hoy.Month);

            var vm = new DashboardViewModel
            {
                NombreUsuario       = ClaimHelper.GetNombreCompleto(User),
                TipoUsuario         = ClaimHelper.GetTipoUsuario(User),
                Mes                 = mesSel,
                Anio                = anioSel,
                PeriodosDisponibles = periodos,
            };

            if (ClaimHelper.EsAdmin(User))
            {
                var boletas   = (await _ventas.ObtenerBoletasDelMesAsync(anioSel, mesSel)).ToList();
                var productos = (await _productos.ObtenerTodosAsync()).ToList();

                vm.TotalBoletasMes           = boletas.Count;
                vm.TotalBoletasPagadasMes    = boletas.Count(b => b.IdEstadoBoleta == 3);
                vm.TotalBoletasAnuladasMes   = boletas.Count(b => b.IdEstadoBoleta == 2);
                vm.TotalBoletasPendientesMes = boletas.Count(b => b.IdEstadoBoleta == 1);
                vm.MontoTotalMes             = boletas.Where(b => b.IdEstadoBoleta == 3).Sum(b => (long)b.MontoTotal);
                vm.TotalProductosBajoStock   = productos.Count(p => p.Stock <= 5 && p.Estado);
                vm.UltimasBoletas            = boletas.Take(15);

                // Productos bajo stock (lista expandible)
                vm.ProductosBajoStock = productos
                    .Where(p => p.Stock <= 5 && p.Estado)
                    .OrderBy(p => p.Stock)
                    .ToList();

                // Ventas por día del mes (boletas pagadas, usando FechaPago)
                int diasEnMes = DateTime.DaysInMonth(anioSel, mesSel);
                var ventasPorDia = new long[diasEnMes];
                foreach (var b in boletas.Where(x => x.IdEstadoBoleta == 3 && x.FechaPago.HasValue))
                    ventasPorDia[b.FechaPago!.Value.Day - 1] += b.MontoTotal;
                vm.VentasPorDiaMes = ventasPorDia;

                // Montos por método de pago (boletas pagadas)
                vm.MontosPorMetodoPago = boletas
                    .Where(b => b.IdEstadoBoleta == 3)
                    .SelectMany(b => b.VenMetodosPagoBoleta)
                    .GroupBy(m => m.IdMetodoPagoNavigation?.NombreMetodoPago ?? "Otro")
                    .ToDictionary(g => g.Key, g => (long)g.Sum(m => m.Monto));
            }
            else if (ClaimHelper.EsVendedor(User))
            {
                var idUsuario = ClaimHelper.GetIdUsuario(User);
                var boletas   = (await _ventas.ObtenerBoletasVendedorDelMesAsync(idUsuario, anioSel, mesSel)).ToList();

                vm.VendedorBoletasMes        = boletas.Count;
                vm.VendedorMontoMes          = boletas.Where(b => b.IdEstadoBoleta == 3).Sum(b => (long)b.MontoTotal);
                vm.VendedorBoletasPendientes = boletas.Count(b => b.IdEstadoBoleta == 1);
                vm.VendedorUltimasBoletas    = boletas.Take(10);
            }
            else if (ClaimHelper.EsCajero(User))
            {
                var idUsuario = ClaimHelper.GetIdUsuario(User);
                var boletas   = (await _ventas.ObtenerBoletasCajeroDelMesAsync(idUsuario, anioSel, mesSel)).ToList();

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
