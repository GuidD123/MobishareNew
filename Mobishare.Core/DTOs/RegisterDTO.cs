using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class RegisterDTO
    {

        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Formato email non valido")]
        [StringLength(255, ErrorMessage = "Email troppo lunga")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Il nome deve essere tra 2 e 100 caratteri")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il cognome è obbligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Il cognome deve essere tra 2 e 100 caratteri")]
        public string Cognome { get; set; } = string.Empty;

        [Required(ErrorMessage = "La password è obbligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La password deve essere tra 8 e 100 caratteri")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La password deve contenere almeno una minuscola, una maiuscola e un numero")]
        public string Password { get; set; } = string.Empty;    
    }
}
