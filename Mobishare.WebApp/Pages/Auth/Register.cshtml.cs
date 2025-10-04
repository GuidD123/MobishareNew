using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IMobishareApiService _apiService;

    public RegisterModel(IMobishareApiService apiService)
    {
        _apiService = apiService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [MinLength(3, ErrorMessage = "Il nome deve avere almeno 3 caratteri")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il cognome è obbligatorio")]
        [MinLength(3, ErrorMessage = "Il cognome deve avere almeno 3 caratteri")]
        [Display(Name = "Cognome")]
        public string Cognome { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Formato email non valido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La password è obbligatoria")]
        [MinLength(8, ErrorMessage = "La password deve avere almeno 8 caratteri")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La password deve contenere almeno una maiuscola e un numero")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La conferma password è obbligatoria")]
        [Compare(nameof(Password), ErrorMessage = "Le password non corrispondono")]
        [DataType(DataType.Password)]
        [Display(Name = "Conferma Password")]
        public string ConfermaPassword { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        // Se l'utente è già loggato, redirect alla home
        if (HttpContext.Session.GetString("JwtToken") != null)
        {
            Response.Redirect("/");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var registerDto = new RegisterDto(
                Nome: Input.Nome,
                Cognome: Input.Cognome,
                Email: Input.Email,
                Password: Input.Password
            );

            var success = await _apiService.RegisterAsync(registerDto);

            if (success)
            {
                TempData["SuccessMessage"] = "Registrazione completata! Effettua il login per accedere.";
                return RedirectToPage("/Auth/Login");
            }
            else
            {
                ErrorMessage = "Registrazione fallita. L'email potrebbe essere già in uso.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore durante la registrazione: {ex.Message}";
            return Page();
        }
    }
}