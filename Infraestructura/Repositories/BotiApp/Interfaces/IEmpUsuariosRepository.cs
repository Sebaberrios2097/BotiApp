using Infraestructura.Entities.BotiApp;

namespace Infraestructura.Repositories.BotiApp.Interfaces
{
    public interface IEmpUsuariosRepository
    {
        Task<EmpUsuario?> GetById(int id);
        Task<EmpUsuario?> GetByNombreUsuario(string nombreUsuario);
        Task<bool?> ToggleEstado(int idUsuario);
    }
}
