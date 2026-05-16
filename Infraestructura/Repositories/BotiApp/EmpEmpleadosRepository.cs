using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp
{
    public class EmpEmpleadoRepository : IEmpEmpleadoRepository
    {
        private readonly BotiAppContext _db;

        public EmpEmpleadoRepository(BotiAppContext db) => _db = db;

        // Obtiene todos los empleados junto a su usuario y tipo de usuario asociado.
        public async Task<IEnumerable<EmpEmpleado>> GetAllWithUsuario()
            => await _db.EmpEmpleado
                        .Include(e => e.EmpUsuario)
                            .ThenInclude(u => u!.IdTipoUsuarioNavigation)
                        .AsNoTracking()
                        .OrderBy(e => e.Apellido1)
                            .ThenBy(e => e.NombresEmpleado)
                        .ToListAsync();

        // Obtiene un empleado por su Id. Retorna null si no existe.
        public async Task<EmpEmpleado?> GetById(int id)
            => await _db.EmpEmpleado
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.IdEmpleado == id);

        // Crea un nuevo empleado y retorna la entidad con el Id generado.
        public async Task<EmpEmpleado> Create(EmpEmpleado empleado)
        {
            _db.EmpEmpleado.Add(empleado);
            await _db.SaveChangesAsync();
            return empleado;
        }
    }
}
