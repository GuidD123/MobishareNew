using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.Core.DTOs;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages.Admin.Corse;

[Authorize(Roles = "Gestore")]
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
    public List<MezzoResponseDTO> MezziDisponibili { get; set; } = new();
    public List<UtenteDTO> UtentiDisponibili { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? FiltroMatricola { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FiltroIdUtente { get; set; }

    // Statistiche generali
    public int TotaleCorse { get; set; }
    public int CorseInCorso { get; set; }
    public int CorseCompletate { get; set; }
    public decimal TotaleIncassato { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            MezziDisponibili = await _apiService.GetMezziAsync() ?? [];
            UtentiDisponibili = await _apiService.GetTuttiUtentiAsync() ?? [];

            // Carica tutte le corse (con eventuali filtri)
            Corse = await _apiService.GetCorseAsync(FiltroIdUtente, FiltroMatricola) ?? [];

            // Ordina per data più recente
            Corse = Corse.OrderByDescending(c => c.DataOraInizio).ToList();

            // Calcola statistiche
            TotaleCorse = Corse.Count;
            CorseInCorso = Corse.Count(c => !c.DataOraFine.HasValue);
            CorseCompletate = TotaleCorse - CorseInCorso;
            TotaleIncassato = Corse.Sum(c => c.CostoFinale ?? 0m);

            _logger.LogInformation("Caricate {Count} corse (filtro: {Filtro})", Corse.Count, FiltroMatricola ?? "nessuno");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel caricamento delle corse per admin");
            ErrorMessage = "Errore nel caricamento delle corse. Riprova più tardi.";
        }

        return Page();
    }
}