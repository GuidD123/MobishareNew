using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La password è obbligatoria")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "La password deve avere almeno 8 caratteri, una maiuscola e un numero")]
        public string Password { get; set; } = string.Empty;
    }
}