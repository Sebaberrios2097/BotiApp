using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp
{
    public class EmpEmpleadoRepository : IEmpEmpleadoRepository
    {
        private readonly BotiAppContext _db;

        public EmpEmpleadoRepository(BotiAppContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Obtiene todos los empleados.
        /// </summary>
        public async Task<IEnumerable<EmpEmpleado>> GetAll()
            => await _db.EmpEmpleado
                        .AsNoTracking()
                        .ToListAsync();

        public async Task<IEnumerable<EmpEmpleado>> GetAllWithUsuario()
            => await _db.EmpEmpleado
                        .Include(e => e.EmpUsuario)
                            .ThenInclude(u => u!.IdTipoUsuarioNavigation)
                        .AsNoTracking()
                        .OrderBy(e => e.Apellido1)
                            .ThenBy(e => e.NombresEmpleado)
                        .ToListAsync();

        /// <summary>
        /// Obtiene un empleado por su Id. Retorna null si no existe.
        /// </summary>
        public async Task<EmpEmpleado?> GetById(int id)
            => await _db.EmpEmpleado
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.IdEmpleado == id);

        /// <summary>
        /// Crea un nuevo empleado y retorna la entidad con el Id generado.
        /// </summary>
        public async Task<EmpEmpleado> Create(EmpEmpleado empleado)
        {
            _db.EmpEmpleado.Add(empleado);
            await _db.SaveChangesAsync();
            return empleado;
        }

        /// <summary>
        /// Actualiza un empleado existente. Retorna null si no existe.
        /// </summary>
        public async Task<EmpEmpleado?> Update(EmpEmpleado empleado)
        {
            var existe = await _db.EmpEmpleado.FindAsync(empleado.IdEmpleado);
            if (existe is null) return null;

            _db.Entry(existe).CurrentValues.SetValues(empleado);
            await _db.SaveChangesAsync();
            return existe;
        }

        /// <summary>
        /// Elimina un empleado por su Id. Retorna false si no existe.
        /// </summary>
        public async Task<bool> Delete(int id)
        {
            var empleado = await _db.EmpEmpleado.FindAsync(id);
            if (empleado is null) return false;

            _db.EmpEmpleado.Remove(empleado);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
