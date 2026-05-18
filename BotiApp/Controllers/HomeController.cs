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
        private readonly IVentasRepository    _ventas;
        private readonly IProductosRepository _productos;
        private readonly IFiadoRepository     _fiado;

        public HomeController(
            IVentasRepository ventas,
            IProductosRepository productos,
            IFiadoRepository fiado)
        {
            _ventas    = ventas;
            _productos = productos;
            _fiado     = fiado;
        }

        // ── Dashboard ─────────────────────────────────────────────────────────
        // Carga KPIs diferenciados según el rol del usuario autenticado.
        public async Task<IActionResult> Index(int? mes, int? anio)
        {
            var vm = await BuildDashboardVmAsync(mes, anio);
            return View(vm);
        }

        // Endpoint AJAX: devuelve los datos del dashboard para el período indicado
        // sin recargar la vista completa.
        [HttpGet]
        public async Task<IActionResult> DashboardDataAjax(int? mes, int? anio)
        {
            var vm = await BuildDashboardVmAsync(mes, anio);

            var mesNombre = System.Globalization.CultureInfo.CurrentCulture
                .DateTimeFormat.GetMonthName(vm.Mes);
            mesNombre = char.ToUpper(mesNombre[0]) + mesNombre[1..];

            return Json(new
            {
                mes      = vm.Mes,
                anio     = vm.Anio,
                mesNombre,
                // ── Admin ──────────────────────────────────────────────
                totalBoletasMes           = vm.TotalBoletasMes,
                totalBoletasPagadasMes    = vm.TotalBoletasPagadasMes,
                totalBoletasPendientesMes = vm.TotalBoletasPendientesMes,
                totalBoletasAnuladasMes   = vm.TotalBoletasAnuladasMes,
                montoTotalMes             = vm.MontoTotalMes,
                totalProductosBajoStock   = vm.TotalProductosBajoStock,
                ventasPorDiaMes           = vm.VentasPorDiaMes,
                diasLabels                = Enumerable
                    .Range(1, vm.VentasPorDiaMes.Length > 0
                        ? vm.VentasPorDiaMes.Length
                        : DateTime.DaysInMonth(vm.Anio, vm.Mes))
                    .Select(d => d.ToString()).ToArray(),
                montosPorMetodoPagoLabels = vm.MontosPorMetodoPago.Keys.ToArray(),
                montosPorMetodoPagoData   = vm.MontosPorMetodoPago.Values.ToArray(),
                productosBajoStock = vm.ProductosBajoStock.Select(p => new
                {
                    nombre = p.NombreProducto,
                    codigo = p.Codigo ?? "—",
                    tipo   = p.IdTipoProductoNavigation?.NombreTipoProducto ?? "—",
                    marca  = p.IdMarcaNavigation?.NombreMarca ?? "—",
                    precio = p.Precio,
                    stock  = p.Stock
                }).ToArray(),
                ultimasBoletas = vm.UltimasBoletas.Select(b => new
                {
                    id           = b.IdBoleta,
                    fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                    vendedor     = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } ve
                                       ? $"{ve.NombresEmpleado} {ve.Apellido1}" : "—",
                    cajero       = b.IdCajeroNavigation?.IdEmpleadoNavigation is { } ce
                                       ? $"{ce.NombresEmpleado} {ce.Apellido1}" : "—",
                    estado       = b.IdEstadoBoleta,
                    monto        = b.MontoTotal
                }).ToArray(),
                // ── Vendedor ───────────────────────────────────────────
                vendedorBoletasMes        = vm.VendedorBoletasMes,
                vendedorMontoMes          = vm.VendedorMontoMes,
                vendedorBoletasPendientes = vm.VendedorBoletasPendientes,
                vendedorUltimasBoletas    = vm.VendedorUltimasBoletas.Select(b => new
                {
                    id           = b.IdBoleta,
                    fechaEmision = b.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                    estado       = b.IdEstadoBoleta,
                    productos    = b.VenBoletaDetalle.Count,
                    monto        = b.MontoTotal
                }).ToArray(),
                // ── Cajero ─────────────────────────────────────────────
                cajeroBoletasCobradas = vm.CajeroBoletasCobradas,
                cajeroBoletasAnuladas = vm.CajeroBoletasAnuladas,
                cajeroMontoGestionado = vm.CajeroMontoGestionado,
                cajeroUltimasBoletas  = vm.CajeroUltimasBoletas.Select(b => new
                {
                    id           = b.IdBoleta,
                    fechaGestion = (b.FechaPago ?? b.FechaEmision)?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                    vendedor     = b.IdVendedorNavigation?.IdEmpleadoNavigation is { } ve
                                       ? $"{ve.NombresEmpleado} {ve.Apellido1}" : "—",
                    estado       = b.IdEstadoBoleta,
                    monto        = b.MontoTotal
                }).ToArray(),
                // ── Fiados ─────────────────────────────────────────────
                totalFiadoGlobal  = vm.TotalFiadoGlobal,
                cantFiadosActivos = vm.CantFiadosActivos
            });
        }

        // Construye el DashboardViewModel completo para el período indicado.
        private async Task<DashboardViewModel> BuildDashboardVmAsync(int? mes, int? anio)
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

            // ── Fiados globales (admin y cajero) ──────────────────────────────
            if (ClaimHelper.EsAdmin(User) || ClaimHelper.EsCajero(User))
            {
                vm.TotalFiadoGlobal  = await _fiado.ObtenerTotalGlobalAdeudadoAsync();
                vm.CantFiadosActivos = await _fiado.ObtenerCantidadClientesConDeudaAsync();
            }

            return vm;
        }

        // ── Vistas estáticas ──────────────────────────────────────────────────
        public IActionResult Privacy()        => View();
        public IActionResult AccesoDenegado() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
