using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;

namespace Mobishare.WebApp.Pages.Mezzi;

public class IndexModel : PageModel
{
    private readonly IMobishareApiService _apiService;

    public IndexModel(IMobishareApiService apiService)
    {
        _apiService = apiService;
    }

    public List<MezzoResponseDTO> Mezzi { get; set; } = new();
    public List<ParcheggioResponseDTO> ParcheggiDisponibili { get; set; } = new();
    public UtenteDTO? UtenteCorrente { get; set; }
    public bool HasCorsaInCorso { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TipoFiltro { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ParcheggioFiltro { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        try
        {
            // Carica dati utente
            UtenteCorrente = await _apiService.GetUtenteAsync(userId.Value);

            // Verifica se ha corsa in corso
            var corse = await _apiService.GetCorseAsync(idUtente: userId.Value);
            HasCorsaInCorso = corse.Any(c => c.DataOraFine == null);

            // Carica mezzi disponibili
            Mezzi = await _apiService.GetMezziDisponibiliAsync();

            // Applica filtri
            if (!string.IsNullOrEmpty(TipoFiltro))
            {
                Mezzi = Mezzi.Where(m => m.Tipo.Equals(TipoFiltro, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (ParcheggioFiltro.HasValue)
            {
                Mezzi = Mezzi.Where(m => m.IdParcheggioCorrente == ParcheggioFiltro.Value).ToList();
            }

            // Carica parcheggi per filtro
            ParcheggiDisponibili = await _apiService.GetParcheggiAsync();

            // Messaggio da TempData
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore nel caricamento dei mezzi: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string matricolaMezzo, int? idParcheggio)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        if (string.IsNullOrEmpty(matricolaMezzo) || !idParcheggio.HasValue)
        {
            TempData["ErrorMessage"] = "Dati mezzo non validi.";
            return RedirectToPage();
        }

        try
        {
            // Verifica credito sufficiente
            var utente = await _apiService.GetUtenteAsync(userId.Value);
            if (utente == null)
            {
                TempData["ErrorMessage"] = "Utente non trovato.";
                return RedirectToPage();
            }

            if (utente.Sospeso)
            {
                TempData["ErrorMessage"] = "Account sospeso. Effettua una ricarica prima di noleggiare.";
                return RedirectToPage();
            }

            // Avvia corsa
            var dto = new AvviaCorsaDTO {
                MatricolaMezzo= matricolaMezzo,
                IdParcheggioPrelievo= idParcheggio.Value
            };

            var corsaAvviata = await _apiService.IniziaCorsaAsync(dto);

            if (corsaAvviata != null)
            {
                TempData["SuccessMessage"] = $"Corsa avviata! Mezzo {matricolaMezzo} noleggiato con successo.";
                return RedirectToPage("/Corse/CorsaCorrente");
            }
            else
            {
                TempData["ErrorMessage"] = "Impossibile avviare la corsa. Il mezzo potrebbe non essere più disponibile.";
                return RedirectToPage();
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Errore durante il noleggio: {ex.Message}";
            return RedirectToPage();
        }
    }
}