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

        /// <summary>
        /// Resetea la contraseña de un usuario (acción de administrador, sin requerir la clave actual).
        /// Devuelve 1=OK, -1=usuario no encontrado.
        /// REQUIERE el procedimiento: Sp_Emp_Reset_Contrasena(@Id_Usuario INT, @Clave_Nueva NVARCHAR(300))
        /// </summary>
        Task<int> SpEmpResetContrasena(int idUsuario, string nuevaClave);
    }
}
