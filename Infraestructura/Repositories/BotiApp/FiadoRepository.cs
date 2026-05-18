using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp;

public class FiadoRepository(BotiAppContext context) : IFiadoRepository
{
    private const int EstadoFiado   = 4;
    private const int EstadoPagada  = 3;

    // ── Clientes ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<FiaClientes>> ObtenerClientesAsync(string? q = null)
    {
        var query = context.FiaClientes
            .AsNoTracking()
            .Include(c => c.VenBoletas)
            .Where(c => c.Estado);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c =>
                c.Nombre.Contains(q) ||
                (c.Telefono != null && c.Telefono.Contains(q)));

        return await query
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<FiaClientes?> ObtenerClientePorIdAsync(int id)
        => await context.FiaClientes
            .AsNoTracking()
            .Include(c => c.VenBoletas)
                .ThenInclude(b => b.IdEstadoBoletaNavigation)
            .Include(c => c.VenBoletas)
                .ThenInclude(b => b.VenBoletaDetalle)
                    .ThenInclude(d => d.IdProductoNavigation)
            .Include(c => c.FiaAbonos)
                .ThenInclude(a => a.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdEmpleadoNavigation)
            .FirstOrDefaultAsync(c => c.IdCliente == id);

public async Task<FiaClientes> CrearClienteAsync(FiaClientes cliente)
    {
        cliente.FechaRegistro = DateTime.Now;
        cliente.Estado = true;
        context.FiaClientes.Add(cliente);
        await context.SaveChangesAsync();
        return cliente;
    }

    public async Task<FiaClientes?> ActualizarClienteAsync(FiaClientes datos)
    {
        var existing = await context.FiaClientes.FindAsync(datos.IdCliente);
        if (existing == null) return null;

        existing.Nombre   = datos.Nombre;
        existing.Telefono = datos.Telefono;

        await context.SaveChangesAsync();
        return existing;
    }

    // ── Boletas fiadas ────────────────────────────────────────────────────────

    /// <summary>Solo boletas con estado Fiado (4) — para cálculo de deuda activa.</summary>
    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasFiadasPorClienteAsync(int idCliente)
        => await context.VenBoletas
            .AsNoTracking()
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdProductoNavigation)
            .Where(b => b.IdClienteFiado == idCliente && b.IdEstadoBoleta == EstadoFiado)
            .OrderBy(b => b.FechaEmision)
            .ToListAsync();

    /// <summary>Todas las boletas alguna vez fiadas al cliente (cualquier estado) — para historial.</summary>
    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasHistorialAsync(int idCliente)
        => await context.VenBoletas
            .AsNoTracking()
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdProductoNavigation)
            .Include(b => b.IdEstadoBoletaNavigation)
            .Where(b => b.IdClienteFiado == idCliente)
            .OrderByDescending(b => b.FechaEmision)
            .ToListAsync();

    // ── Abonos ────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<FiaAbonos>> RegistrarAbonoAsync(
        int idCliente, int idUsuario, IEnumerable<AbonoMetodoItem> metodos, string? observaciones = null)
    {
        var metodosList = metodos.ToList();
        if (metodosList.Count == 0)
            throw new ArgumentException("Debe especificar al menos un método de pago.");

        var totalMonto = metodosList.Sum(m => m.Monto);
        if (totalMonto <= 0)
            throw new ArgumentException("El monto total del abono debe ser mayor a 0.");

        await using var tx = await context.Database.BeginTransactionAsync();

        var cliente = await context.FiaClientes
            .Include(c => c.VenBoletas)
            .FirstOrDefaultAsync(c => c.IdCliente == idCliente && c.Estado)
            ?? throw new InvalidOperationException($"Cliente fiado {idCliente} no encontrado.");

        var fechaAbono = DateTime.Now;
        var abonos = new List<FiaAbonos>();

        // 1. Crear un registro FiaAbonos por cada método de pago
        foreach (var m in metodosList)
        {
            var abono = new FiaAbonos
            {
                IdCliente     = idCliente,
                IdUsuario     = idUsuario,
                IdMetodoPago  = m.IdMetodoPago,
                Monto         = m.Monto,
                Fecha         = fechaAbono,
                Observaciones = observaciones
            };
            context.FiaAbonos.Add(abono);
            abonos.Add(abono);
        }

        // 2. Acreditar saldo a favor con el total
        cliente.SaldoAFavor += totalMonto;

        // 3. FIFO auto-pago: pagar boletas más antiguas mientras alcance el saldo
        var boletasPendientes = cliente.VenBoletas
            .Where(b => b.IdEstadoBoleta == EstadoFiado)
            .OrderBy(b => b.FechaEmision)
            .ToList();

        foreach (var boleta in boletasPendientes)
        {
            if (cliente.SaldoAFavor < boleta.MontoTotal) break;

            cliente.SaldoAFavor  -= boleta.MontoTotal;
            boleta.IdEstadoBoleta = EstadoPagada;
            boleta.FechaPago      = fechaAbono;
            boleta.IdCajero       = idUsuario;
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return abonos;
    }

    public async Task<IEnumerable<FiaAbonos>> ObtenerAbonosPorClienteAsync(int idCliente)
        => await context.FiaAbonos
            .AsNoTracking()
            .Include(a => a.IdUsuarioNavigation).ThenInclude(u => u.IdEmpleadoNavigation)
            .Where(a => a.IdCliente == idCliente)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();

    // ── Dashboard / globales ──────────────────────────────────────────────────

    public async Task<int> ObtenerTotalGlobalAdeudadoAsync()
        => await context.VenBoletas
            .Where(b => b.IdEstadoBoleta == EstadoFiado)
            .SumAsync(b => (int?)b.MontoTotal) ?? 0;

    public async Task<int> ObtenerCantidadClientesConDeudaAsync()
        => await context.VenBoletas
            .Where(b => b.IdEstadoBoleta == EstadoFiado && b.IdClienteFiado != null)
            .Select(b => b.IdClienteFiado)
            .Distinct()
            .CountAsync();
}
