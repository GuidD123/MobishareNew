using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;

namespace Mobishare.WebApp.Pages.Corse;

public class IndexModel : PageModel
{
    private readonly IMobishareApiService _apiService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IMobishareApiService apiService, ILogger<IndexModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public List<CorsaResponseDTO> Corse { get; set; } = new();
    public List<ParcheggioResponseDTO> Parcheggi { get; set; } = new();
    public int CorseCompletate { get; set; }
    public decimal TotaleSpeso { get; set; }
    public int PuntiBonus { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Verifica autenticazione
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var profilo = await _apiService.GetProfiloUtenteAsync();
        if (profilo != null)
            PuntiBonus = profilo.PuntiBonus;

        try
        {
            // Carica tutte le corse dell'utente (già ordinate)
            Corse = (await _apiService.GetStoricoCorseUtenteAsync(userId.Value))
                .OrderByDescending(c => c.DataOraInizio)
                .ToList();

            // Carica parcheggi
            Parcheggi = await _apiService.GetParcheggiAsync();

            // Calcola statistiche
            CorseCompletate = Corse.Count(c => c.DataOraFine.HasValue);
            TotaleSpeso = Corse
                .Where(c => c.CostoFinale.HasValue)
                .Sum(c => c.CostoFinale!.Value);

            // Leggi messaggi da TempData (se presenti)
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }
            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel caricamento delle corse per utente {UserId}", userId);
            ErrorMessage = "Errore nel caricamento delle corse. Riprova più tardi.";
            Corse = new List<CorsaResponseDTO>();
            Parcheggi = new List<ParcheggioResponseDTO>();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostTerminaCorsaAsync(int idCorsa, int idParcheggioRilascio)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Validazione input
        if (idCorsa <= 0 || idParcheggioRilascio <= 0)
        {
            TempData["ErrorMessage"] = "Parametri non validi.";
            return RedirectToPage();
        }

        try
        {
            // Verifica che la corsa appartenga all'utente (opzionale, dipende dall'API)
            // var corsa = await _apiService.GetCorsaByIdAsync(idCorsa);
            // if (corsa == null || corsa.IdUtente != userId.Value)
            // {
            //     TempData["ErrorMessage"] = "Corsa non trovata o non autorizzato.";
            //     return RedirectToPage();
            // }

            var dto = new FineCorsaDTO
            {
                IdParcheggioRilascio = idParcheggioRilascio,
                DataOraFineCorsa = DateTime.Now,
                SegnalazioneProblema = false
            };

            var result = await _apiService.TerminaCorsaAsync(idCorsa, dto);

            if (result != null)
            {
                TempData["SuccessMessage"] = "Corsa terminata correttamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Impossibile terminare la corsa.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Errore HTTP nella terminazione corsa {CorsaId}", idCorsa);
            TempData["ErrorMessage"] = "Errore di connessione. Riprova più tardi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore imprevisto nella terminazione corsa {CorsaId}", idCorsa);
            TempData["ErrorMessage"] = "Si è verificato un errore imprevisto. Riprova più tardi.";
        }

        return RedirectToPage();
    }
}