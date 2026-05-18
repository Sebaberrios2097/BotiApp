using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces
{
    public interface IEmpEmpleadoRepository
    {
        Task<IEnumerable<EmpEmpleado>> GetAllWithUsuario();
        Task<EmpEmpleado?> GetById(int id);
        Task<EmpEmpleado> Create(EmpEmpleado empleado);
        Task<EmpEmpleado?> Update(int idEmpleado, string nombres, string apellido1, string? apellido2, string? fono, string? correo);
    }
}
