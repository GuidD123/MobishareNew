using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;

namespace Mobishare.WebApp.Pages.Admin.Corse;

public class CorseTotModel : PageModel
{
    private readonly IMobishareApiService _apiService;
    private readonly ILogger<CorseTotModel> _logger;

    public CorseTotModel(IMobishareApiService apiService, ILogger<CorseTotModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public List<CorsaResponseDTO> Corse { get; set; } = new();

    // Proprietà per i filtri
    [BindProperty(SupportsGet = true)]
    public int? FiltroIdUtente { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FiltroMatricola { get; set; }

    // Statistiche generali
    public int TotaleCorse { get; set; }
    public int CorseInCorso { get; set; }
    public int CorseCompletate { get; set; }
    public decimal TotaleIncassato { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Verifica autenticazione e ruolo
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (userRole != "Gestore")
        {
            TempData["ErrorMessage"] = "Non hai i permessi per accedere a questa pagina.";
            return RedirectToPage("/Index");
        }

        try
        {
            // Carica tutte le corse (con eventuali filtri)
            Corse = await _apiService.GetCorseAsync(FiltroIdUtente, FiltroMatricola);

            // Ordina per data più recente
            Corse = Corse.OrderByDescending(c => c.DataOraInizio).ToList();

            // Calcola statistiche
            TotaleCorse = Corse.Count;
            CorseInCorso = Corse.Count(c => !c.DataOraFine.HasValue);
            CorseCompletate = Corse.Count(c => c.DataOraFine.HasValue);
            TotaleIncassato = Corse
                .Where(c => c.CostoFinale.HasValue)
                .Sum(c => c.CostoFinale!.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel caricamento delle corse per admin");
            ErrorMessage = "Errore nel caricamento delle corse. Riprova più tardi.";
            Corse = new List<CorsaResponseDTO>();
        }

        return Page();
    }
}