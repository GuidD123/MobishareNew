using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;

namespace Mobishare.WebApp.Pages.DashboardAdmin;

public class GestioneMezziModel : PageModel
{
    private readonly IMobishareApiService _apiService;
    private readonly ILogger<GestioneMezziModel> _logger;

    public GestioneMezziModel(IMobishareApiService apiService, ILogger<GestioneMezziModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public List<MezzoResponseDTO> Mezzi { get; set; } = new();
    public List<ParcheggioResponseDTO> Parcheggi { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string Matricola { get; set; } = string.Empty;

    [BindProperty]
    public string Tipo { get; set; } = string.Empty;

    [BindProperty]
    public int IdParcheggioCorrente { get; set; }

    [BindProperty]
    public int? LivelloBatteria { get; set; }

    [BindProperty]
    public string Stato { get; set; } = "Disponibile";

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

        await CaricaDatiAsync();

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

        if (string.IsNullOrWhiteSpace(Matricola))
        {
            ErrorMessage = "La matricola è obbligatoria.";
            await CaricaDatiAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Tipo))
        {
            ErrorMessage = "Il tipo di mezzo è obbligatorio.";
            await CaricaDatiAsync();
            return Page();
        }

        if (IdParcheggioCorrente <= 0)
        {
            ErrorMessage = "Seleziona un parcheggio valido.";
            await CaricaDatiAsync();
            return Page();
        }

        if (LivelloBatteria.HasValue && (LivelloBatteria < 0 || LivelloBatteria > 100))
        {
            ErrorMessage = "Il livello di batteria deve essere tra 0 e 100.";
            await CaricaDatiAsync();
            return Page();
        }

        try
        {
            // Mappa Stato stringa -> enum
            StatoMezzo statoEnum = Stato switch
            {
                "Disponibile" => StatoMezzo.Disponibile,
                "Manutenzione" => StatoMezzo.Manutenzione,
                "NonPrelevabile" => StatoMezzo.NonPrelevabile,
                _ => StatoMezzo.Disponibile
            };

            // Mappa Tipo stringa -> enum
            TipoMezzo tipoEnum = Tipo switch
            {
                "Bicicletta" => TipoMezzo.BiciMuscolare,
                "BiciclettaElettrica" => TipoMezzo.BiciElettrica,
                "MonopattinoElettrico" => TipoMezzo.MonopattinoElettrico,
                _ => TipoMezzo.BiciMuscolare
            };

            var dto = new MezzoCreateDTO
            {
                Matricola = Matricola.Trim().ToUpper(),
                Tipo = tipoEnum,
                IdParcheggioCorrente = IdParcheggioCorrente,
                LivelloBatteria = LivelloBatteria ?? 0,
                Stato = statoEnum
            };

            _logger.LogInformation("Tentativo di creazione mezzo {Matricola} da utente {UserId}", Matricola, userId);

            var result = await _apiService.CreaMezzoAsync(dto);

            if (result != null)
            {
                _logger.LogInformation("Mezzo {Matricola} creato con successo con ID {Id}", Matricola, result.Id);
                TempData["SuccessMessage"] = $"Mezzo '{Matricola}' aggiunto con successo!";
                return RedirectToPage();
            }
            else
            {
                _logger.LogError("CreaMezzoAsync ha restituito null per {Matricola}", Matricola);
                ErrorMessage = _apiService.LastError ?? "Errore durante la creazione del mezzo.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Errore HTTP durante la creazione del mezzo {Matricola}", Matricola);
            ErrorMessage = _apiService.LastError ?? "Errore di connessione. Riprova tra qualche istante.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generico durante la creazione del mezzo {Matricola}", Matricola);
            ErrorMessage = $"Errore imprevisto: {ex.Message}";
        }

        await CaricaDatiAsync();
        return Page();
    }

    private async Task CaricaDatiAsync()
    {
        try
        {
            // Carica tutti i mezzi (endpoint GetMezzi - solo per Gestore)
            Mezzi = await _apiService.GetMezziAsync();
            _logger.LogInformation("Caricati {Count} mezzi", Mezzi.Count);

            // Carica parcheggi per dropdown
            Parcheggi = await _apiService.GetParcheggiAsync();
            _logger.LogInformation("Caricati {Count} parcheggi", Parcheggi.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento dei dati");
            Mezzi = new List<MezzoResponseDTO>();
            Parcheggi = new List<ParcheggioResponseDTO>();

            if (string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = "Errore nel caricamento dei dati.";
            }
        }
    }
}