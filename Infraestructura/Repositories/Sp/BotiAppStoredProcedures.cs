using Infraestructura.Context;
using Infraestructura.Repositories.Sp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Repositories.Sp
{
    public class BotiAppStoredProcedures : IBotiAppStoredProcedures
    {
        private readonly BotiAppContext db;
        public BotiAppStoredProcedures(BotiAppContext db)
        {
            this.db = db;
        }
        public async Task<string> SpEmpCreaUsuarioEmpleado(int rut)
        {
            var resultado = db.Database
                .SqlQueryRaw<string>("EXEC Sp_Emp_Crea_UsuarioEmpleado {0}", rut)
                .AsEnumerable()
                .FirstOrDefault();

            return resultado ?? "Hubo un error al crear el usuario.";
        }

        public async Task<int> SpEmpValidaAccesoUsuario(string usuario, string clave)
        {
            try
            {
                var resultado = db.Database
                    .SqlQueryRaw<int>("EXEC Sp_Emp_Valida_AccesoUsuario {0}, {1}", usuario, clave)
                    .AsEnumerable()
                    .FirstOrDefault();

                return resultado;
            }
            catch (Exception ex)
            {
                // Muestra toda la cadena de errores
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Inner: {ex.InnerException?.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
