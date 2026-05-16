using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces
{
    public interface IEmpEmpleadoRepository
    {
        Task<IEnumerable<EmpEmpleado>> GetAllWithUsuario();
        Task<EmpEmpleado?> GetById(int id);
        Task<EmpEmpleado> Create(EmpEmpleado empleado);
    }
}
