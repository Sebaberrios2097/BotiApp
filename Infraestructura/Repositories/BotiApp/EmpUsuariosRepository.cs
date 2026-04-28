using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp
{
    public class EmpUsuariosRepository : IEmpUsuariosRepository
    {
        private readonly BotiAppContext _db;

        public EmpUsuariosRepository(BotiAppContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Obtiene todos los usuarios.
        /// </summary>
        public async Task<IEnumerable<EmpUsuario>> GetAll()
            => await _db.EmpUsuario
                        .AsNoTracking()
                        .ToListAsync();

        /// <summary>
        /// Obtiene un usuario por su Id. Retorna null si no existe.
        /// </summary>
        public async Task<EmpUsuario?> GetById(int id)
            => await _db.EmpUsuario
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.IdUsuario == id);

        /// <summary>
        /// Obtiene un usuario por su nombre. Retorna null si no existe.
        /// </summary>
        public async Task<EmpUsuario?> GetByNombreUsuario(string nombreUsuario)
            => await _db.EmpUsuario
                        .Include(e => e.IdEmpleadoNavigation)
                        .Include(e => e.IdTipoUsuarioNavigation)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.NombreUsuario == nombreUsuario);

        /// <summary>
        /// Crea un nuevo usuario y retorna la entidad con el Id generado.
        /// </summary>
        public async Task<EmpUsuario> Create(EmpUsuario usuario)
        {
            _db.EmpUsuario.Add(usuario);
            await _db.SaveChangesAsync();
            return usuario;
        }

        /// <summary>
        /// Actualiza un usuario existente. Retorna null si no existe.
        /// </summary>
        public async Task<EmpUsuario?> Update(EmpUsuario usuario)
        {
            var existe = await _db.EmpUsuario.FindAsync(usuario.IdUsuario);
            if (existe is null) return null;

            _db.Entry(existe).CurrentValues.SetValues(usuario);
            await _db.SaveChangesAsync();
            return existe;
        }

        /// <summary>
        /// Elimina un usuario por su Id. Retorna false si no existe.
        /// </summary>
        public async Task<bool> Delete(int id)
        {
            var usuario = await _db.EmpUsuario.FindAsync(id);
            if (usuario is null) return false;

            _db.EmpUsuario.Remove(usuario);
            await _db.SaveChangesAsync();
            return true;
        }

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
