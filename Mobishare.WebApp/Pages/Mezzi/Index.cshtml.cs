using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;

namespace Mobishare.WebApp.Pages.Mezzi;

public class IndexModel : PageModel
{
    private readonly IMobishareApiService _apiService;
    private readonly ILogger<IndexModel> _logger;

    //Soglia minima batteria per mezzi elettrici (20%)
    private const int SOGLIA_BATTERIA_MINIMA = 20;

    public IndexModel(IMobishareApiService apiService, ILogger<IndexModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
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
            _logger.LogWarning("Tentativo di accesso a Mezzi senza autenticazione");
            return RedirectToPage("/Account/Login");
        }

        try
        {
            // Carica dati utente
            UtenteCorrente = await _apiService.GetUtenteAsync(userId.Value);
            _logger.LogInformation("Utente {UserId} caricato, Sospeso: {Sospeso}", userId.Value, UtenteCorrente?.Sospeso);

            // Verifica se ha corsa in corso
            var corse = await _apiService.GetCorseAsync(idUtente: userId.Value);
            HasCorsaInCorso = corse.Any(c => c.DataOraFine == null);
            _logger.LogInformation("Utente {UserId} ha corsa in corso: {HasCorsa}", userId.Value, HasCorsaInCorso);

            // Carica mezzi disponibili
            var mezziDallApi = await _apiService.GetMezziDisponibiliAsync();
            _logger.LogInformation("Caricati {Count} mezzi dall'API", mezziDallApi.Count);

            // ⭐ FILTRO MEZZI CON BATTERIA SCARICA ⭐
            // Escludi mezzi elettrici con batteria < 20%
            Mezzi = mezziDallApi.Where(m => IsMezzoPrelevabile(m)).ToList();

            var mezziFiltrati = mezziDallApi.Count - Mezzi.Count;
            if (mezziFiltrati > 0)
            {
                _logger.LogInformation("Filtrati {Count} mezzi con batteria scarica (< {Soglia}%)",
                    mezziFiltrati, SOGLIA_BATTERIA_MINIMA);
            }

            // Applica filtri utente
            if (!string.IsNullOrEmpty(TipoFiltro))
            {
                Mezzi = Mezzi.Where(m => m.Tipo.Equals(TipoFiltro, StringComparison.OrdinalIgnoreCase)).ToList();
                _logger.LogInformation("Applicato filtro tipo: {Tipo}, risultati: {Count}", TipoFiltro, Mezzi.Count);
            }

            if (ParcheggioFiltro.HasValue)
            {
                Mezzi = Mezzi.Where(m => m.IdParcheggioCorrente == ParcheggioFiltro.Value).ToList();
                _logger.LogInformation("Applicato filtro parcheggio: {IdParcheggio}, risultati: {Count}",
                    ParcheggioFiltro.Value, Mezzi.Count);
            }

            // Carica parcheggi per filtro
            ParcheggiDisponibili = await _apiService.GetParcheggiAsync();

            // Messaggio da TempData
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }
            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"]?.ToString();
            }

            _logger.LogInformation("Pagina Mezzi caricata con successo. Mezzi visibili: {Count}", Mezzi.Count);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Errore di connessione durante il caricamento dei mezzi");
            ErrorMessage = "Errore di connessione al server. Verifica la tua connessione e riprova.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generico durante il caricamento dei mezzi");
            ErrorMessage = $"Errore nel caricamento dei mezzi: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string matricolaMezzo, int? idParcheggio)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            _logger.LogWarning("Tentativo di noleggio senza autenticazione");
            return RedirectToPage("/Account/Login");
        }

        if (string.IsNullOrEmpty(matricolaMezzo) || !idParcheggio.HasValue)
        {
            _logger.LogWarning("Tentativo di noleggio con dati non validi. Matricola: {Matricola}, IdParcheggio: {IdParcheggio}",
                matricolaMezzo, idParcheggio);
            TempData["ErrorMessage"] = "Dati mezzo non validi.";
            return RedirectToPage();
        }

        try
        {
            // Verifica credito sufficiente
            var utente = await _apiService.GetUtenteAsync(userId.Value);
            if (utente == null)
            {
                _logger.LogError("Utente {UserId} non trovato durante il noleggio", userId.Value);
                TempData["ErrorMessage"] = "Utente non trovato.";
                return RedirectToPage();
            }

            if (utente.Sospeso)
            {
                _logger.LogWarning("Tentativo di noleggio da utente sospeso {UserId}", userId.Value);
                TempData["ErrorMessage"] = "Account sospeso. Effettua una ricarica prima di noleggiare.";
                return RedirectToPage();
            }

            // Verifica che il mezzo sia ancora disponibile e prelevabile
            var mezziDisponibili = await _apiService.GetMezziDisponibiliAsync();
            var mezzo = mezziDisponibili.FirstOrDefault(m => m.Matricola == matricolaMezzo);

            if (mezzo == null)
            {
                _logger.LogWarning("Mezzo {Matricola} non trovato o non più disponibile", matricolaMezzo);
                TempData["ErrorMessage"] = "Il mezzo selezionato non è più disponibile.";
                return RedirectToPage();
            }

            // Verifica batteria prima di noleggiare
            if (!IsMezzoPrelevabile(mezzo))
            {
                _logger.LogWarning("Tentativo di noleggio mezzo {Matricola} con batteria insufficiente ({Batteria}%)",
                    matricolaMezzo, mezzo.LivelloBatteria);
                TempData["ErrorMessage"] = "Il mezzo selezionato ha la batteria scarica e non può essere noleggiato.";
                return RedirectToPage();
            }

            // Avvia corsa
            var dto = new AvviaCorsaDTO
            {
                MatricolaMezzo = matricolaMezzo,
                IdParcheggioPrelievo = idParcheggio.Value
            };

            _logger.LogInformation("Tentativo di avvio corsa per utente {UserId}, mezzo {Matricola}",
                userId.Value, matricolaMezzo);

            var corsaAvviata = await _apiService.IniziaCorsaAsync(dto);

            if (corsaAvviata != null)
            {
                _logger.LogInformation("Corsa {IdCorsa} avviata con successo per utente {UserId}, mezzo {Matricola}",
                    corsaAvviata.Id, userId.Value, matricolaMezzo);
                TempData["SuccessMessage"] = $"Corsa avviata! Mezzo {matricolaMezzo} noleggiato con successo.";
                return RedirectToPage("/Corse/CorsaCorrente");
            }
            else
            {
                _logger.LogError("IniziaCorsaAsync ha restituito null per utente {UserId}, mezzo {Matricola}",
                    userId.Value, matricolaMezzo);
                TempData["ErrorMessage"] = "Impossibile avviare la corsa. Il mezzo potrebbe non essere più disponibile.";
                return RedirectToPage();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Errore di connessione durante il noleggio del mezzo {Matricola}", matricolaMezzo);
            TempData["ErrorMessage"] = "Errore di connessione. Riprova tra qualche istante.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il noleggio del mezzo {Matricola} per utente {UserId}",
                matricolaMezzo, userId);
            TempData["ErrorMessage"] = $"Errore durante il noleggio: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostSegnalaGuastoAsync(string matricolaMezzo)
    {
        try
        {
            var success = await _apiService.SegnalaGuastoAsync(matricolaMezzo);
            if (success)
                TempData["SuccessMessage"] = "Guasto segnalato con successo. Grazie per la segnalazione.";
            else
                TempData["ErrorMessage"] = _apiService.LastError ?? "Errore nella segnalazione del guasto.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la segnalazione guasto per mezzo {Id}", matricolaMezzo);
            TempData["ErrorMessage"] = "Errore imprevisto durante la segnalazione.";
        }

        return RedirectToPage();
    }


    /// <summary>
    /// Verifica se un mezzo è prelevabile in base al tipo e al livello di batteria
    /// </summary>
    /// <param name="mezzo">Il mezzo da verificare</param>
    /// <returns>True se il mezzo è prelevabile, False altrimenti</returns>
    private bool IsMezzoPrelevabile(MezzoResponseDTO mezzo)
    {
        // Se il mezzo ha la batteria, verifica che sia sopra la soglia minima
        if (mezzo.LivelloBatteria.HasValue)
        {
            // Verifica se è un mezzo elettrico (ha batteria)
            var tipoMezzoLower = mezzo.Tipo.ToLower();
            bool isMezzoElettrico = tipoMezzoLower.Contains("elettric") ||
                                    tipoMezzoLower.Contains("ebike") ||
                                    tipoMezzoLower.Contains("escooter");

            if (isMezzoElettrico && mezzo.LivelloBatteria.Value < SOGLIA_BATTERIA_MINIMA)
            {
                _logger.LogDebug("Mezzo {Matricola} filtrato: batteria {Batteria}% < soglia {Soglia}%",
                    mezzo.Matricola, mezzo.LivelloBatteria.Value, SOGLIA_BATTERIA_MINIMA);
                return false;
            }
        }

        return true;
    }
}