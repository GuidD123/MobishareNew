using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;

namespace Mobishare.WebApp.Pages.Corse;

public class IndexModel : PageModel
{
    private readonly IMobishareApiService _apiService;

    public IndexModel(IMobishareApiService apiService)
    {
        _apiService = apiService;
    }

    public List<CorsaResponseDTO> Corse { get; set; } = new();
    public int CorseCompletate { get; set; }
    public decimal TotaleSpeso { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Verifica autenticazione
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        try
        {
            // Carica tutte le corse dell'utente
            Corse = await _apiService.GetStoricoCorseUtenteAsync(userId.Value);

            // Calcola statistiche
            CorseCompletate = Corse.Count(c => c.DataOraFine.HasValue);
            TotaleSpeso = Corse
                .Where(c => c.CostoFinale.HasValue)
                .Sum(c => c.CostoFinale!.Value);

            // Messaggio da TempData
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore nel caricamento delle corse: {ex.Message}";
        }

        return Page();
    }
}