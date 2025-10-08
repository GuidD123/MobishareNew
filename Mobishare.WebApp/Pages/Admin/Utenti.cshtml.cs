using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs; 

namespace Mobishare.WebApp.Pages.Admin;

public class UtentiModel : PageModel
{
    private readonly IMobishareApiService _apiService;

    public UtentiModel(IMobishareApiService apiService)
    {
        _apiService = apiService;
    }

    public List<UtenteDTO> TuttiUtenti { get; set; } = new();
    public List<UtenteDTO> UtentiSospesi { get; set; } = new();
    public bool MostraSoloSospesi { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(bool soloSospesi = false)
    {
        // Verifica autenticazione
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        // Verifica ruolo Gestore
        var userRole = HttpContext.Session.GetString("UserRole");
        if (!userRole?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            TempData["ErrorMessage"] = "Solo i gestori possono accedere a questa pagina.";
            return RedirectToPage("/Dashboard/Index");
        }

        MostraSoloSospesi = soloSospesi;

        try
        {
            // Carica tutti gli utenti
            TuttiUtenti = await _apiService.GetTuttiUtentiAsync();

            // Carica utenti sospesi
            UtentiSospesi = await _apiService.GetUtentiSospesiAsync();

            // Messaggio da TempData
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore nel caricamento degli utenti: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRiattivaAsync(int idUtente)
    {
        // Verifica autenticazione
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        // Verifica ruolo Gestore
        var userRole = HttpContext.Session.GetString("UserRole");
        if (!userRole?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            TempData["ErrorMessage"] = "Solo i gestori possono riattivare utenti.";
            return RedirectToPage("/Dashboard/Index");
        }

        try
        {
            var success = await _apiService.RiattivaUtenteAsync(idUtente);

            if (success)
            {
                TempData["SuccessMessage"] = $"Utente #{idUtente} riattivato con successo!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Impossibile riattivare l'utente #{idUtente}.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Errore durante la riattivazione: {ex.Message}";
        }

        return RedirectToPage(new { soloSospesi = MostraSoloSospesi });
    }
}