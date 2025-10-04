using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using System.ComponentModel.DataAnnotations;

namespace Mobishare.WebApp.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IMobishareApiService _apiService;

        public LoginModel(IMobishareApiService apiService)
        {
            _apiService = apiService;
        }

        [BindProperty]
        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "La password è obbligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Controlla se l'utente è già loggato
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                // Già loggato, redirect alla dashboard
                Response.Redirect("/Dashboard");
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
                var loginResult = await _apiService.LoginAsync(Email, Password);

                if (loginResult == null)
                {
                    ErrorMessage = "Email o password non corretti";
                    return Page();
                }

                // Salva il token e i dati utente in Session
                HttpContext.Session.SetString("JwtToken", loginResult.Token);
                HttpContext.Session.SetInt32("UserId", loginResult.Id);
                HttpContext.Session.SetString("UserName", loginResult.Nome);
                HttpContext.Session.SetString("UserRole", loginResult.Ruolo);

                // Redirect in base al ruolo
                if (loginResult.Ruolo == "Gestore")
                {
                    return RedirectToPage("/Admin/Dashboard");
                }
                else
                {
                    return RedirectToPage("/Dashboard/Index");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Errore durante il login. Riprova più tardi.";
                // Log dell'errore se hai un logger
                return Page();
            }
        }
    }
}

/*Dependency Injection di IMobishareApiService
BindProperty su Email e Password per il binding automatico dal form
Validation con Data Annotations
Session per salvare token e dati utente
Redirect differenziato per Gestore vs Utente normale
Controllo in OnGet per evitare che un utente già loggato riacceda alla pagina login*/