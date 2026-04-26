using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces
{
    public interface IEmpEmpleadoRepository
    {
        Task<IEnumerable<EmpEmpleado>> GetAll();
        Task<EmpEmpleado?> GetById(int id);
        Task<EmpEmpleado> Create(EmpEmpleado empleado);
        Task<EmpEmpleado?> Update(EmpEmpleado empleado);
        Task<bool> Delete(int id);
    }
}