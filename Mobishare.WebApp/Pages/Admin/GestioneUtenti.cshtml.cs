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
            return RedirectToPage("/Account/Login");
        }

        // Verifica ruolo Gestore
        var userRole = HttpContext.Session.GetString("UserRole");
        if (!userRole?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            TempData["ErrorMessage"] = "Solo i gestori possono accedere a questa pagina.";
            return RedirectToPage("/DashboardAdmin/Index");
        }

        MostraSoloSospesi = soloSospesi;

        try
        {
            // Carica tutti gli utenti
            TuttiUtenti = await _apiService.GetTuttiUtentiAsync();
            Console.WriteLine($"Utenti caricati: {TuttiUtenti.Count}");

            // Carica utenti sospesi
            UtentiSospesi = await _apiService.GetUtentiSospesiAsync();
            Console.WriteLine($"UtentiSospesi caricati: {UtentiSospesi.Count}");

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
            return RedirectToPage("/Account/Login");
        }

        // Verifica ruolo Gestore
        var userRole = HttpContext.Session.GetString("UserRole");
        if (!userRole?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            TempData["ErrorMessage"] = "Solo i gestori possono riattivare utenti.";
            return RedirectToPage("/DashboardAdmin/Index");
        }

        try
        {
            var success = await _apiService.RiattivaUtenteAsync(idUtente);

            if (TempData.ContainsKey("SuccessMessage"))
            {
                SuccessMessage = TempData.Peek("SuccessMessage")?.ToString();
            }

            if (TempData.ContainsKey("ErrorMessage"))
            {
                ErrorMessage = TempData.Peek("ErrorMessage")?.ToString();
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Errore durante la riattivazione: {ex.Message}";
        }

        return RedirectToPage("/Admin/GestioneUtenti", new { soloSospesi = UtentiSospesi.Count > 1 });
    }
}