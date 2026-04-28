using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces
{
    public interface IEmpUsuariosRepository
    {
        Task<IEnumerable<EmpUsuario>> GetAll();
        Task<EmpUsuario?> GetById(int id);
        Task<EmpUsuario?> GetByNombreUsuario(string nombreUsuario);
        Task<EmpUsuario> Create(EmpUsuario usuario);
        Task<EmpUsuario?> Update(EmpUsuario usuario);
        Task<bool> Delete(int id);
        Task<bool?> ToggleEstado(int idUsuario);
    }
}
