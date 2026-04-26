namespace BotiApp.Models
{
    public class LoginViewModel
    {
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public bool Recordar { get; set; }
    }
}
