namespace Infraestructura.Repositories.Sp.Interfaces
{
    public interface IBotiAppStoredProcedures
    {
        Task<string> SpEmpCreaUsuarioEmpleado(int rut);
        Task<int> SpEmpValidaAccesoUsuario(string usuario, string clave);
    }
}
