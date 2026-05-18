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
            .Where(c => c.Estado);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c =>
                c.Nombres.Contains(q) ||
                c.Apellido1.Contains(q) ||
                (c.Apellido2 != null && c.Apellido2.Contains(q)) ||
                (c.Telefono != null && c.Telefono.Contains(q)) ||
                c.Rut.ToString().Contains(q));

        return await query
            .OrderBy(c => c.Apellido1)
            .ThenBy(c => c.Nombres)
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

    public async Task<FiaClientes?> ObtenerClientePorRutAsync(int rut)
        => await context.FiaClientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Rut == rut);

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

        existing.Nombres      = datos.Nombres;
        existing.Apellido1    = datos.Apellido1;
        existing.Apellido2    = datos.Apellido2;
        existing.Telefono     = datos.Telefono;
        existing.Observaciones = datos.Observaciones;

        await context.SaveChangesAsync();
        return existing;
    }

    // ── Boletas fiadas ────────────────────────────────────────────────────────

    public async Task<IEnumerable<VenBoletas>> ObtenerBoletasFiadasPorClienteAsync(int idCliente)
        => await context.VenBoletas
            .AsNoTracking()
            .Include(b => b.VenBoletaDetalle).ThenInclude(d => d.IdProductoNavigation)
            .Where(b => b.IdClienteFiado == idCliente && b.IdEstadoBoleta == EstadoFiado)
            .OrderBy(b => b.FechaEmision)
            .ToListAsync();

    // ── Abonos ────────────────────────────────────────────────────────────────

    public async Task<FiaAbonos> RegistrarAbonoAsync(
        int idCliente, int idUsuario, int monto, string? observaciones = null)
    {
        await using var tx = await context.Database.BeginTransactionAsync();

        var cliente = await context.FiaClientes
            .Include(c => c.VenBoletas)
            .FirstOrDefaultAsync(c => c.IdCliente == idCliente && c.Estado)
            ?? throw new InvalidOperationException($"Cliente fiado {idCliente} no encontrado.");

        // 1. Registrar abono histórico (siempre monto completo)
        var abono = new FiaAbonos
        {
            IdCliente     = idCliente,
            IdUsuario     = idUsuario,
            Monto         = monto,
            Fecha         = DateTime.Now,
            Observaciones = observaciones
        };
        context.FiaAbonos.Add(abono);

        // 2. Acreditar saldo a favor
        cliente.SaldoAFavor += monto;

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
            boleta.FechaPago      = DateTime.Now;
            boleta.IdCajero       = idUsuario;
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return abono;
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
