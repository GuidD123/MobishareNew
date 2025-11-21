using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages.DashboardAdmin;

public class GestioneParcheggiModel : PageModel
{
    private readonly IMobishareApiService _apiService;
    private readonly ILogger<GestioneParcheggiModel> _logger;

    public GestioneParcheggiModel(IMobishareApiService apiService, ILogger<GestioneParcheggiModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public List<ParcheggioResponseDTO> Parcheggi { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string Nome { get; set; } = string.Empty;

    [BindProperty]
    public string Zona { get; set; } = string.Empty;

    [BindProperty]
    public string? Indirizzo { get; set; }

    [BindProperty]
    public int Capienza { get; set; }

    [BindProperty]
    public bool Attivo { get; set; } = true;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!userRole?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            TempData["ErrorMessage"] = "Non hai i permessi per accedere a questa pagina.";
            return RedirectToPage("/DashboardAdmin/Index");
        }

        await CaricaParcheggiAsync();

        if (TempData["SuccessMessage"] != null)
        {
            SuccessMessage = TempData["SuccessMessage"]?.ToString();
        }
        if (TempData["ErrorMessage"] != null)
        {
            ErrorMessage = TempData["ErrorMessage"]?.ToString();
        }

        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!userRole?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            TempData["ErrorMessage"] = "Non hai i permessi per eseguire questa operazione.";
            return RedirectToPage("/DashboardAdmin/Index");
        }

        if (string.IsNullOrWhiteSpace(Nome))
        {
            ErrorMessage = "Il nome del parcheggio è obbligatorio.";
            await CaricaParcheggiAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Zona))
        {
            ErrorMessage = "La zona è obbligatoria.";
            await CaricaParcheggiAsync();
            return Page();
        }

        if (Capienza <= 0)
        {
            ErrorMessage = "La capienza deve essere maggiore di zero.";
            await CaricaParcheggiAsync();
            return Page();
        }

        try
        {
            var dto = new ParcheggioCreateDTO
            {
                Nome = Nome.Trim(),
                Zona = Zona.Trim(),
                Indirizzo = string.IsNullOrWhiteSpace(Indirizzo) ? null : Indirizzo.Trim(),
                Capienza = Capienza,
                Attivo = Attivo
            };

            var result = await _apiService.CreaParcheggioAsync(dto);

            if (result != null)
            {
                TempData["SuccessMessage"] = $"Parcheggio '{Nome}' aggiunto con successo!";
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = "Errore durante la creazione del parcheggio.";
            }
        }
        catch (HttpRequestException ex)
        {
            if (ex.Message.Contains("409") || ex.Message.Contains("Conflict"))
            {
                ErrorMessage = $"Esiste già un parcheggio con il nome '{Nome}'.";
            }
            else if (ex.Message.Contains("400") || ex.Message.Contains("Bad Request"))
            {
                ErrorMessage = "Dati non validi. Verifica i campi inseriti.";
            }
            else
            {
                ErrorMessage = "Errore di connessione. Riprova tra qualche istante.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore imprevisto: {ex.Message}";
        }

        await CaricaParcheggiAsync();
        return Page();
    }
    public async Task<IActionResult> OnPostAggiornaStatoAsync(int id, bool attivo)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (!userRole?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            TempData["ErrorMessage"] = "Non hai i permessi per modificare lo stato dei parcheggi.";
            return RedirectToPage("/DashboardAdmin/Index");
        }

        try
        {
            var success = await _apiService.AggiornaStatoParcheggioAsync(id, attivo);

            if (success)
            {
                TempData["SuccessMessage"] = attivo
                    ? "Parcheggio riattivato correttamente."
                    : "Parcheggio disattivato correttamente.";
            }
            else
            {
                TempData["ErrorMessage"] = _apiService.LastError ?? "Errore nell'aggiornamento stato parcheggio.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore in OnPostAggiornaStatoAsync");
            TempData["ErrorMessage"] = "Errore di connessione o server non raggiungibile.";
        }

        return RedirectToPage();
    }
    private async Task CaricaParcheggiAsync()
    {
        try
        {
            Parcheggi = await _apiService.GetParcheggiAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento dei parcheggi");
            Parcheggi = new List<ParcheggioResponseDTO>();
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = "Errore nel caricamento dei parcheggi esistenti.";
            }
        }
    }
    public async Task<IActionResult> OnPostEliminaAsync(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null)
            return RedirectToPage("/Account/Login");

        if (!userRole?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            TempData["ErrorMessage"] = "Non hai i permessi per eseguire questa operazione.";
            return RedirectToPage("/DashboardAdmin/Index");
        }

        try
        {
            // Recupera il parcheggio per ottenere il nome (per il messaggio)
            var parcheggio = Parcheggi.FirstOrDefault(p => p.Id == id);

            // Se non è in cache, ricarica
            if (parcheggio == null)
            {
                await CaricaParcheggiAsync();
                parcheggio = Parcheggi.FirstOrDefault(p => p.Id == id);
            }

            if (parcheggio == null)
            {
                TempData["ErrorMessage"] = "Parcheggio non trovato.";
                return RedirectToPage();
            }

            // Chiama API per eliminazione
            var success = await _apiService.EliminaParcheggioAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = $"Parcheggio '{parcheggio.Nome}' eliminato con successo dal sistema.";
                _logger.LogWarning("Parcheggio {Nome} (ID {Id}) eliminato dal gestore {UserId}",
                    parcheggio.Nome, id, userId);
            }
            else
            {
                // L'errore specifico è già in _apiService.LastError
                TempData["ErrorMessage"] = _apiService.LastError ??
                    "Errore durante l'eliminazione del parcheggio. Potrebbe contenere mezzi o essere referenziato in corse storiche.";

                _logger.LogWarning("Tentativo fallito di eliminare parcheggio {Nome}: {Errore}",
                    parcheggio.Nome, _apiService.LastError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione del parcheggio {Id}", id);
            TempData["ErrorMessage"] = $"Errore imprevisto: {ex.Message}";
        }

        return RedirectToPage();
    }

}