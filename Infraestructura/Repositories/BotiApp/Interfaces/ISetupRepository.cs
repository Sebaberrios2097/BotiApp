namespace Infraestructura.Repositories.BotiApp.Interfaces
{
    public interface ISetupRepository
    {
        /// <summary>
        /// Devuelve true si no existe ningún usuario de tipo Administrador.
        /// </summary>
        Task<bool> RequiereSetupAsync();

        /// <summary>
        /// Crea los tipos de usuario (Administrador, Vendedor, Cajero) si no existen.
        /// Devuelve el Id del tipo Administrador.
        /// </summary>
        Task<int> EnsureTiposUsuarioAsync();
    }
}
