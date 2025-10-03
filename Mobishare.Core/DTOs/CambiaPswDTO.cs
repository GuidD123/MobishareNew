using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class CambiaPswDTO
    {
        [Required]
        [EmailAddress(ErrorMessage = "Formato email non valido")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string VecchiaPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "La nuova password deve avere almeno 8 caratteri")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La password deve contenere almeno una maiuscola e un numero")]
        public string NuovaPassword { get; set; } = string.Empty;   
    }
}
