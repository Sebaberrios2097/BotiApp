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
            // Cualquier rol que incluya Vendedor (puro o combinado, ej. Cajero/Vendedor)
            // no ve el dashboard: su vista de inicio es Generar Venta.
            if (ClaimHelper.EsVendedor(User) && !ClaimHelper.EsAdmin(User))
            {
                return RedirectToAction("Generar", "Ventas", new { area = "Ventas" });
            }

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
                totalBoletasFiadasMes     = vm.TotalBoletasFiadasMes,
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
                ventasPorCategoriaLabels  = vm.VentasPorCategoria.Keys.ToArray(),
                ventasPorCategoriaData    = vm.VentasPorCategoria.Values.ToArray(),
                productosMasVendidos      = vm.ProductosMasVendidos.Select(p => new { nombre = p.Nombre, codigo = p.Codigo, cantidad = p.Cantidad }).ToArray(),
                productosMenosVendidos    = vm.ProductosMenosVendidos.Select(p => new { nombre = p.Nombre, codigo = p.Codigo, cantidad = p.Cantidad }).ToArray(),
                cantidadCigarrosVendidos  = vm.CantidadCigarrosVendidos,
                montoCigarrosVendidos     = vm.MontoCigarrosVendidos,
                metodosPagoCigarros       = vm.MetodosPagoCigarros.Select(m => new { metodoPago = m.MetodoPago, monto = m.Monto, porcentaje = m.Porcentaje }).ToArray(),
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
                vm.TotalBoletasFiadasMes     = boletas.Count(b => b.IdEstadoBoleta == 4);
                vm.MontoTotalMes             = boletas.Where(b => b.IdEstadoBoleta == 3).Sum(b => (long)b.MontoTotal);
                vm.TotalProductosBajoStock   = productos.Count(p => p.Stock <= 5 && p.Estado);
                vm.UltimasBoletas            = boletas.OrderByDescending(b => b.IdBoleta).Take(50);

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

                // ── Mapeos de búsqueda rápida ──────────────────────────────────
                var prodTipoNombreMap = productos.ToDictionary(p => p.IdProducto, p => p.IdTipoProductoNavigation?.NombreTipoProducto ?? "Otro");
                var prodTipoIdMap = productos.ToDictionary(p => p.IdProducto, p => p.IdTipoProducto);
                var prodNombreMap = productos.ToDictionary(p => p.IdProducto, p => p.NombreProducto);
                var prodCodigoMap = productos.ToDictionary(p => p.IdProducto, p => p.Codigo ?? "—");

                // 1. Ventas por categoría (gráfico)
                vm.VentasPorCategoria = boletas
                    .Where(b => b.IdEstadoBoleta == 3)
                    .SelectMany(b => b.VenBoletaDetalle)
                    .GroupBy(d => prodTipoNombreMap.TryGetValue(d.IdProducto, out var catName) ? catName : "Otro")
                    .ToDictionary(g => g.Key, g => (long)g.Sum(d => d.Subtotal));

                // 2. Tops de productos más y menos vendidos (5 de cada uno)
                var salesByProduct = boletas
                    .Where(b => b.IdEstadoBoleta == 3)
                    .SelectMany(b => b.VenBoletaDetalle)
                    .GroupBy(d => d.IdProducto)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

                var activeProductsWithSales = productos
                    .Where(p => p.Estado)
                    .Select(p => new ProductoVendidoViewModel
                    {
                        IdProducto = p.IdProducto,
                        Nombre = p.NombreProducto,
                        Codigo = p.Codigo ?? "—",
                        Cantidad = salesByProduct.GetValueOrDefault(p.IdProducto, 0),
                        Monto = boletas
                            .Where(b => b.IdEstadoBoleta == 3)
                            .SelectMany(b => b.VenBoletaDetalle)
                            .Where(d => d.IdProducto == p.IdProducto)
                            .Sum(d => (long)d.Subtotal)
                    })
                    .ToList();

                vm.ProductosMasVendidos = activeProductsWithSales
                    .OrderByDescending(p => p.Cantidad)
                    .ThenBy(p => p.Nombre)
                    .Take(5)
                    .ToList();

                vm.ProductosMenosVendidos = activeProductsWithSales
                    .OrderBy(p => p.Cantidad)
                    .ThenBy(p => p.Nombre)
                    .Take(5)
                    .ToList();

                // 3. Ventas de la categoría Cigarros (ID = -1)
                var cigaretteDetails = boletas
                    .Where(b => b.IdEstadoBoleta == 3)
                    .SelectMany(b => b.VenBoletaDetalle)
                    .Where(d => prodTipoIdMap.TryGetValue(d.IdProducto, out var catId) && catId == -1)
                    .ToList();

                vm.CantidadCigarrosVendidos = cigaretteDetails.Sum(d => d.Cantidad);
                vm.MontoCigarrosVendidos = cigaretteDetails.Sum(d => (long)d.Subtotal);

                // 4. Métodos de pago para la categoría Cigarros
                var pagosCigarros = new Dictionary<string, long>();
                foreach (var b in boletas.Where(b => b.IdEstadoBoleta == 3))
                {
                    var cigDetalles = b.VenBoletaDetalle
                        .Where(d => prodTipoIdMap.TryGetValue(d.IdProducto, out var catId) && catId == -1)
                        .ToList();
                    if (!cigDetalles.Any()) continue;

                    long montoCigarrosEnBoleta = cigDetalles.Sum(d => (long)d.Subtotal);
                    double totalBoleta = b.MontoTotal;
                    if (totalBoleta <= 0) continue;

                    foreach (var mp in b.VenMetodosPagoBoleta)
                    {
                        var nombreMetodo = mp.IdMetodoPagoNavigation?.NombreMetodoPago ?? "Otro";
                        double ratio = mp.Monto / totalBoleta;
                        long montoAsignado = (long)Math.Round(montoCigarrosEnBoleta * ratio);

                        if (pagosCigarros.ContainsKey(nombreMetodo))
                            pagosCigarros[nombreMetodo] += montoAsignado;
                        else
                            pagosCigarros[nombreMetodo] = montoAsignado;
                    }
                }

                long totalMontoCigarros = pagosCigarros.Values.Sum();
                vm.MetodosPagoCigarros = pagosCigarros.Select(kvp => new MetodoPagoCigarrosViewModel
                {
                    MetodoPago = kvp.Key,
                    Monto = kvp.Value,
                    Porcentaje = totalMontoCigarros > 0 ? (kvp.Value * 100.0 / totalMontoCigarros) : 0.0
                })
                .OrderByDescending(x => x.Monto)
                .ToList();
            }
            else
            {
                // No son "else if": un usuario con rol combinado (ej. Cajero/Vendedor)
                // debe ver los KPI de ambos roles, no solo del primero que matchee.
                if (ClaimHelper.EsVendedor(User))
                {
                    var idUsuario = ClaimHelper.GetIdUsuario(User);
                    var boletas   = (await _ventas.ObtenerBoletasVendedorDelMesAsync(idUsuario, anioSel, mesSel)).ToList();

                    vm.VendedorBoletasMes        = boletas.Count;
                    vm.VendedorMontoMes          = boletas.Where(b => b.IdEstadoBoleta == 3).Sum(b => (long)b.MontoTotal);
                    vm.VendedorBoletasPendientes = boletas.Count(b => b.IdEstadoBoleta == 1);
                    vm.VendedorUltimasBoletas    = boletas.Take(10);
                }

                if (ClaimHelper.EsCajero(User))
                {
                    var idUsuario = ClaimHelper.GetIdUsuario(User);
                    var boletas   = (await _ventas.ObtenerBoletasCajeroDelMesAsync(idUsuario, anioSel, mesSel)).ToList();

                    vm.CajeroBoletasCobradas = boletas.Count(b => b.IdEstadoBoleta == 3);
                    vm.CajeroBoletasAnuladas = boletas.Count(b => b.IdEstadoBoleta == 2);
                    vm.CajeroMontoGestionado = boletas.Where(b => b.IdEstadoBoleta == 3).Sum(b => (long)b.MontoTotal);
                    vm.CajeroUltimasBoletas  = boletas.Take(10);
                }
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
