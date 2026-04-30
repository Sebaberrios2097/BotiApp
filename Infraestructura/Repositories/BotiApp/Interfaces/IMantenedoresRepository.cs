using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces
{
    public interface IMantenedoresRepository
    {
        // ── Tipos de Producto ───────────────────────────────────────────────
        Task<IEnumerable<ProTiposProductos>> GetTiposProductoAsync();
        Task<ProTiposProductos> CreateTipoProductoAsync(string nombre);
        Task<ProTiposProductos?> UpdateTipoProductoAsync(int id, string nombre);
        Task<bool> DeleteTipoProductoAsync(int id);

        // ── Marcas ──────────────────────────────────────────────────────────
        Task<IEnumerable<ProMarcas>> GetMarcasAsync();
        Task<ProMarcas> CreateMarcaAsync(string nombre);
        Task<ProMarcas?> UpdateMarcaAsync(int id, string nombre);
        Task<bool?> ToggleMarcaAsync(int id);
        Task<bool> DeleteMarcaAsync(int id);

        // ── Métodos de Pago ─────────────────────────────────────────────────
        Task<IEnumerable<VenMetodosPago>> GetMetodosPagoAsync();
        Task<VenMetodosPago> CreateMetodoPagoAsync(string nombre);
        Task<VenMetodosPago?> UpdateMetodoPagoAsync(int id, string nombre);
        Task<bool> DeleteMetodoPagoAsync(int id);
    }
}
