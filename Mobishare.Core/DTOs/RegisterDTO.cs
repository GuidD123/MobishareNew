using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class RegisterDto
    {

        [Required]
        [EmailAddress(ErrorMessage = "Formato email non valido")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(3, ErrorMessage = "Il nome deve avere almeno 3 caratteri")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [MinLength(3, ErrorMessage = "Il cognome deve avere almeno 3 caratteri")]
        public string Cognome { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "La password deve avere almeno 8 caratteri")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Almeno una maiuscola e un numero richiesti")]
        public string Password { get; set; } = string.Empty;    
    }
}
