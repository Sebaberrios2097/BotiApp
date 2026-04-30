namespace Infraestructura.Repositories.Sp.Interfaces
{
    public interface IBotiAppStoredProcedures
    {
        Task<string> SpEmpCreaUsuarioEmpleado(int rut, int idTipoUsuario);
        Task<int> SpEmpValidaAccesoUsuario(string usuario, string clave);
        /// <summary>
        /// Cambia la contraseña del usuario. Devuelve 1=OK, 0=clave actual incorrecta, -1=usuario no encontrado.
        /// </summary>
        Task<int> SpEmpCambiaContrasena(string usuario, string claveActual, string claveNueva);
    }
}
