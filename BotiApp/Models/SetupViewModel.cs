using System.ComponentModel.DataAnnotations;

namespace BotiApp.Models
{
    public class SetupViewModel
    {
        // ── Datos del empleado ──────────────────────────────────────────────

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
        [Display(Name = "Nombres")]
        public string NombresEmpleado { get; set; } = string.Empty;

        [Required(ErrorMessage = "El primer apellido es obligatorio.")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        [Display(Name = "Primer apellido")]
        public string Apellido1 { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        [Display(Name = "Segundo apellido")]
        public string? Apellido2 { get; set; }

        [Required(ErrorMessage = "El RUT es obligatorio.")]
        [Range(1000000, 99999999, ErrorMessage = "RUT inválido (sin puntos ni dígito verificador).")]
        [Display(Name = "RUT (sin puntos ni dígito verificador)")]
        public int Rut { get; set; }

        [Phone(ErrorMessage = "Teléfono inválido.")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        [Display(Name = "Teléfono")]
        public string? Fono { get; set; }

        [EmailAddress(ErrorMessage = "Correo inválido.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string? Correo { get; set; }
    }
}
