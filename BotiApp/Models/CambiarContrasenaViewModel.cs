using System.ComponentModel.DataAnnotations;

namespace BotiApp.Models
{
    public class CambiarContrasenaViewModel
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [Display(Name = "Usuario")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string ClaveActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string ClaveNueva { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la nueva contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(ClaveNueva), ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string ConfirmarClave { get; set; } = string.Empty;
    }
}
