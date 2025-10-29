using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;

namespace Mobishare.WebApp.Pages.Corse;

public class FeedbackUtenteModel : PageModel
{
    private readonly IMobishareApiService _apiService;
    private readonly ILogger<FeedbackUtenteModel> _logger;

    public FeedbackUtenteModel(IMobishareApiService apiService, ILogger<FeedbackUtenteModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public CorsaResponseDTO? CorsaTerminata { get; set; }
    public int DurataMinuti { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public int IdCorsa { get; set; }

    [BindProperty]
    public int Valutazione { get; set; }

    [BindProperty]
    public string? Commento { get; set; }

    public async Task<IActionResult> OnGetAsync(int idCorsa)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            CorsaTerminata = await _apiService.GetCorsaAsync(idCorsa);

            if (CorsaTerminata == null)
            {
                TempData["ErrorMessage"] = "Corsa non trovata.";
                return RedirectToPage("/Corse/Index");
            }

            if (CorsaTerminata.IdUtente != userId.Value)
            {
                TempData["ErrorMessage"] = "Non hai i permessi per valutare questa corsa.";
                return RedirectToPage("/Corse/Index");
            }

            if (!CorsaTerminata.DataOraFine.HasValue)
            {
                TempData["ErrorMessage"] = "La corsa non è ancora terminata.";
                return RedirectToPage("/Corse/CorsaCorrente");
            }

            var durata = CorsaTerminata.DataOraFine.Value - CorsaTerminata.DataOraInizio;
            DurataMinuti = (int)durata.TotalMinutes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel caricamento della corsa {IdCorsa}", idCorsa);
            TempData["ErrorMessage"] = "Errore nel caricamento dei dati della corsa.";
            return RedirectToPage("/Corse/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (Valutazione < 1 || Valutazione > 5)
        {
            ErrorMessage = "La valutazione deve essere tra 1 e 5 stelle.";
            await OnGetAsync(IdCorsa);
            return Page();
        }

        try
        {
            var dto = new FeedbackCreateDTO
            {
                IdUtente = userId.Value,
                IdCorsa = IdCorsa,
                Valutazione = Valutazione,
                Commento = string.IsNullOrWhiteSpace(Commento) ? null : Commento.Trim()
            };

            var success = await _apiService.InviaFeedbackAsync(dto);

            if (success)
            {
                TempData["SuccessMessage"] = "Grazie per il tuo feedback!";
                return RedirectToPage("/Corse/Index");
            }
            else
            {
                ErrorMessage = _apiService.LastError ?? "Errore durante l'invio del feedback.";
                await OnGetAsync(IdCorsa);
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio del feedback per corsa {IdCorsa}", IdCorsa);
            ErrorMessage = "Errore durante l'invio del feedback. Riprova.";
            await OnGetAsync(IdCorsa);
            return Page();
        }
    }
}