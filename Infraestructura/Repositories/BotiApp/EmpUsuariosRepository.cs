using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp
{
    public class EmpUsuariosRepository : IEmpUsuariosRepository
    {
        private readonly BotiAppContext _db;

        public EmpUsuariosRepository(BotiAppContext db) => _db = db;

        // Obtiene un usuario por su Id. Retorna null si no existe.
        public async Task<EmpUsuario?> GetById(int id)
            => await _db.EmpUsuario
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.IdUsuario == id);

        // Obtiene un usuario por nombre de usuario, incluyendo empleado y tipo.
        // Usado en el flujo de autenticación.
        public async Task<EmpUsuario?> GetByNombreUsuario(string nombreUsuario)
            => await _db.EmpUsuario
                        .Include(e => e.IdEmpleadoNavigation)
                        .Include(e => e.IdTipoUsuarioNavigation)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.NombreUsuario == nombreUsuario);

        // Activa o desactiva un usuario. Retorna el nuevo estado o null si no existe.
        public async Task<bool?> ToggleEstado(int idUsuario)
        {
            var usuario = await _db.EmpUsuario.FindAsync(idUsuario);
            if (usuario is null) return null;

            usuario.Estado = !usuario.Estado;
            await _db.SaveChangesAsync();
            return usuario.Estado;
        }
    }
}
