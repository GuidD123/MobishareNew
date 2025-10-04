using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages.Profilo;

public class IndexModel : PageModel
{
    private readonly IMobishareApiService _apiService;

    public IndexModel(IMobishareApiService apiService)
    {
        _apiService = apiService;
    }

    public UtenteDto? Utente { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public AggiornaNomeInput InputNome { get; set; } = new();

    [BindProperty]
    public CambiaPasswordInput InputPassword { get; set; } = new();

    public class AggiornaNomeInput
    {
        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [MinLength(3, ErrorMessage = "Il nome deve avere almeno 3 caratteri")]
        [Display(Name = "Nuovo Nome")]
        public string NuovoNome { get; set; } = string.Empty;

        [Required(ErrorMessage = "La password è obbligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Password attuale")]
        public string Password { get; set; } = string.Empty;
    }

    public class CambiaPasswordInput
    {
        [Required(ErrorMessage = "La password attuale è obbligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Attuale")]
        public string VecchiaPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nuova password è obbligatoria")]
        [MinLength(8, ErrorMessage = "La password deve avere almeno 8 caratteri")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La password deve contenere almeno una maiuscola e un numero")]
        [DataType(DataType.Password)]
        [Display(Name = "Nuova Password")]
        public string NuovaPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La conferma password è obbligatoria")]
        [Compare(nameof(NuovaPassword), ErrorMessage = "Le password non corrispondono")]
        [DataType(DataType.Password)]
        [Display(Name = "Conferma Password")]
        public string ConfermaPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Verifica autenticazione
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        // Carica dati utente
        Utente = await _apiService.GetUtenteAsync(userId.Value);

        if (Utente == null)
        {
            ErrorMessage = "Impossibile caricare i dati dell'utente.";
        }

        // Messaggio da TempData (se presente)
        if (TempData["SuccessMessage"] != null)
        {
            SuccessMessage = TempData["SuccessMessage"]?.ToString();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAggiornaNomeAsync()
    {
        // Ricarica dati utente per visualizzazione
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        Utente = await _apiService.GetUtenteAsync(userId.Value);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var dto = new AggiornaProfiloDto(
                Nome: InputNome.NuovoNome,
                Password: InputNome.Password
            );

            var success = await _apiService.AggiornaProfiloAsync(userId.Value, dto);

            if (success)
            {
                SuccessMessage = "Nome aggiornato con successo!";
                // Aggiorna anche il nome in sessione se presente
                HttpContext.Session.SetString("UserName", InputNome.NuovoNome);

                // Ricarica dati aggiornati
                Utente = await _apiService.GetUtenteAsync(userId.Value);
                ModelState.Clear();
                InputNome = new();
            }
            else
            {
                ErrorMessage = "Impossibile aggiornare il nome. Verifica la password.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore durante l'aggiornamento: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCambiaPasswordAsync()
    {
        // Ricarica dati utente per visualizzazione
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        Utente = await _apiService.GetUtenteAsync(userId.Value);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var dto = new CambiaPswDto(
                VecchiaPassword: InputPassword.VecchiaPassword,
                NuovaPassword: InputPassword.NuovaPassword
            );

            var success = await _apiService.CambiaPasswordAsync(dto);

            if (success)
            {
                SuccessMessage = "Password cambiata con successo!";
                ModelState.Clear();
                InputPassword = new();
            }
            else
            {
                ErrorMessage = "Impossibile cambiare la password. Verifica la password attuale.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore durante il cambio password: {ex.Message}";
        }

        return Page();
    }
}