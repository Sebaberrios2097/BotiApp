using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp
{
    public class MantenedoresRepository : IMantenedoresRepository
    {
        private readonly BotiAppContext _db;
        public MantenedoresRepository(BotiAppContext db) => _db = db;

        // ── Tipos de Producto ───────────────────────────────────────────────

        public async Task<IEnumerable<ProTiposProductos>> GetTiposProductoAsync()
            => await _db.ProTiposProductos
                        .AsNoTracking()
                        .OrderBy(t => t.NombreTipoProducto)
                        .ToListAsync();

        public async Task<ProTiposProductos> CreateTipoProductoAsync(string nombre)
        {
            var entidad = new ProTiposProductos { NombreTipoProducto = nombre.Trim() };
            _db.ProTiposProductos.Add(entidad);
            await _db.SaveChangesAsync();
            return entidad;
        }

        public async Task<ProTiposProductos?> UpdateTipoProductoAsync(int id, string nombre)
        {
            var entidad = await _db.ProTiposProductos.FindAsync(id);
            if (entidad is null) return null;
            entidad.NombreTipoProducto = nombre.Trim();
            await _db.SaveChangesAsync();
            return entidad;
        }

        public async Task<bool> DeleteTipoProductoAsync(int id)
        {
            var entidad = await _db.ProTiposProductos.FindAsync(id);
            if (entidad is null) return false;
            _db.ProTiposProductos.Remove(entidad);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── Marcas ──────────────────────────────────────────────────────────

        public async Task<IEnumerable<ProMarcas>> GetMarcasAsync()
            => await _db.ProMarcas
                        .AsNoTracking()
                        .OrderBy(m => m.NombreMarca)
                        .ToListAsync();

        public async Task<ProMarcas> CreateMarcaAsync(string nombre)
        {
            var entidad = new ProMarcas { NombreMarca = nombre.Trim(), Estado = true };
            _db.ProMarcas.Add(entidad);
            await _db.SaveChangesAsync();
            return entidad;
        }

        public async Task<ProMarcas?> UpdateMarcaAsync(int id, string nombre)
        {
            var entidad = await _db.ProMarcas.FindAsync(id);
            if (entidad is null) return null;
            entidad.NombreMarca = nombre.Trim();
            await _db.SaveChangesAsync();
            return entidad;
        }

        public async Task<bool?> ToggleMarcaAsync(int id)
        {
            var entidad = await _db.ProMarcas.FindAsync(id);
            if (entidad is null) return null;
            entidad.Estado = !entidad.Estado;
            await _db.SaveChangesAsync();
            return entidad.Estado;
        }

        public async Task<bool> DeleteMarcaAsync(int id)
        {
            var entidad = await _db.ProMarcas.FindAsync(id);
            if (entidad is null) return false;
            _db.ProMarcas.Remove(entidad);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── Métodos de Pago ─────────────────────────────────────────────────

        public async Task<IEnumerable<VenMetodosPago>> GetMetodosPagoAsync()
            => await _db.VenMetodosPago
                        .AsNoTracking()
                        .OrderBy(m => m.NombreMetodoPago)
                        .ToListAsync();

        public async Task<VenMetodosPago> CreateMetodoPagoAsync(string nombre)
        {
            var entidad = new VenMetodosPago { NombreMetodoPago = nombre.Trim() };
            _db.VenMetodosPago.Add(entidad);
            await _db.SaveChangesAsync();
            return entidad;
        }

        public async Task<VenMetodosPago?> UpdateMetodoPagoAsync(int id, string nombre)
        {
            var entidad = await _db.VenMetodosPago.FindAsync(id);
            if (entidad is null) return null;
            entidad.NombreMetodoPago = nombre.Trim();
            await _db.SaveChangesAsync();
            return entidad;
        }

        public async Task<bool> DeleteMetodoPagoAsync(int id)
        {
            var entidad = await _db.VenMetodosPago.FindAsync(id);
            if (entidad is null) return false;
            _db.VenMetodosPago.Remove(entidad);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
