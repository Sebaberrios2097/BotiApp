using Infraestructura.Context;
using Infraestructura.Entities.BotiApp;
using Infraestructura.Repositories.BotiApp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.BotiApp
{
    public class SetupRepository : ISetupRepository
    {
        private readonly BotiAppContext _db;

        public SetupRepository(BotiAppContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Devuelve true si no existe ningún usuario de tipo Administrador.
        /// </summary>
        public async Task<bool> RequiereSetupAsync()
        {
            return !await _db.EmpUsuario
                .AnyAsync(u => u.IdTipoUsuarioNavigation.NombreTipoUsuario == "Administrador");
        }

        /// <summary>
        /// Crea los tipos de usuario Administrador, Vendedor y Cajero si no existen.
        /// Devuelve el Id del tipo Administrador.
        /// </summary>
        public async Task<int> EnsureTiposUsuarioAsync()
        {
            var tiposExistentes = await _db.EmpTiposUsuario.ToListAsync();

            var nombresRequeridos = new[] { "Administrador", "Vendedor", "Cajero" };

            foreach (var nombre in nombresRequeridos)
            {
                if (!tiposExistentes.Any(t => t.NombreTipoUsuario == nombre))
                {
                    _db.EmpTiposUsuario.Add(new EmpTiposUsuario { NombreTipoUsuario = nombre });
                }
            }

            await _db.SaveChangesAsync();

            // Recuperar el Id del tipo Administrador (puede haberse creado en este ciclo)
            var admin = await _db.EmpTiposUsuario
                .FirstAsync(t => t.NombreTipoUsuario == "Administrador");

            return admin.IdTipoUsuario;
        }
    }
}
