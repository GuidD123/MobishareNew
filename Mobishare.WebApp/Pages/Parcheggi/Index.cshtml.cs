using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;

namespace Mobishare.WebApp.Pages.Parcheggi;

public class IndexModel : PageModel
{
    private readonly IMobishareApiService _apiService;

    public IndexModel(IMobishareApiService apiService)
    {
        _apiService = apiService;
    }

    public List<ParcheggioResponseDTO> Parcheggi { get; set; } = new();
    public List<string> ZoneUniche { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ZonaFiltro { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SoloAttivi { get; set; } = true;

    public async Task<IActionResult> OnGetAsync()
    {
        // Verifica autenticazione
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            // Carica tutti i parcheggi
            var tuttiParcheggi = await _apiService.GetParcheggiAsync();

            if (tuttiParcheggi == null || tuttiParcheggi.Count == 0)
            {
                ErrorMessage = "Nessun parcheggio disponibile al momento.";
                return Page();
            }

            // Estrai zone uniche per il filtro
            ZoneUniche = tuttiParcheggi
                .Select(p => p.Zona)
                .Distinct()
                .OrderBy(z => z)
                .ToList();

            // Applica filtri
            Parcheggi = tuttiParcheggi;

            // Filtro per zona
            if (!string.IsNullOrEmpty(ZonaFiltro))
            {
                Parcheggi = Parcheggi
                    .Where(p => p.Zona.Equals(ZonaFiltro, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Filtro solo attivi
            if (SoloAttivi)
            {
                Parcheggi = Parcheggi.Where(p => p.Attivo).ToList();
            }

            // Ordina per zona e nome
            Parcheggi = Parcheggi
                .OrderBy(p => p.Id)
                .ThenBy(p => p.Nome)
                .ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore nel caricamento dei parcheggi: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAggiornaStatoAsync(int id, bool attivo)
    {
        var success = await _apiService.AggiornaStatoParcheggioAsync(id, attivo);

        if (success)
            TempData["SuccessMessage"] = attivo
                ? "Parcheggio riattivato correttamente."
                : "Parcheggio disattivato correttamente.";
        else
            TempData["ErrorMessage"] = _apiService.LastError ?? "Errore nell'aggiornamento stato parcheggio.";

        return RedirectToPage();
    }
}