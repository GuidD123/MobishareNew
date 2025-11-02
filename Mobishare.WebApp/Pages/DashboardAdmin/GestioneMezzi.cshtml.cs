using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Models;
using Mobishare.WebApp.Services;

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

    [BindProperty]
    public int IdMezzoSposta { get; set; }

    [BindProperty]
    public int NuovoParcheggioId { get; set; }

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
                "Bicicletta Elettrica" => TipoMezzo.BiciElettrica,
                "Monopattino Elettrico" => TipoMezzo.MonopattinoElettrico,
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

    public async Task<IActionResult> OnPostSpostaAsync()
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

        if (IdMezzoSposta <= 0)
        {
            TempData["ErrorMessage"] = "Mezzo non valido.";
            return RedirectToPage();
        }

        if (NuovoParcheggioId <= 0)
        {
            TempData["ErrorMessage"] = "Seleziona un parcheggio valido.";
            return RedirectToPage();
        }

        try
        {
            // Recupera il mezzo corrente per mantenere stato e batteria
            var mezzoCorrente = await _apiService.GetMezzoAsync(IdMezzoSposta);

            if (mezzoCorrente == null)
            {
                TempData["ErrorMessage"] = "Mezzo non trovato.";
                return RedirectToPage();
            }

            // Prepara DTO con lo spostamento
            var dto = new MezzoUpdateDTO
            {
                LivelloBatteria = mezzoCorrente.LivelloBatteria ?? 0,
                Stato = Enum.Parse<StatoMezzo>(mezzoCorrente.Stato),
                IdParcheggioCorrente = NuovoParcheggioId
            };

            var success = await _apiService.SpostaMezzoAsync(IdMezzoSposta, dto);

            if (success)
            {
                var parcheggioDestinazione = (await _apiService.GetParcheggiAsync())
                    .FirstOrDefault(p => p.Id == NuovoParcheggioId);

                TempData["SuccessMessage"] = $"Mezzo '{mezzoCorrente.Matricola}' spostato con successo in '{parcheggioDestinazione?.Nome}'!";
            }
            else
            {
                TempData["ErrorMessage"] = _apiService.LastError ?? "Errore durante lo spostamento del mezzo.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lo spostamento del mezzo {Id}", IdMezzoSposta);
            TempData["ErrorMessage"] = $"Errore imprevisto: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRipristinaAsync(int id)
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
            // Recupera il mezzo esistente
            var mezzo = await _apiService.GetMezzoAsync(id);
            if (mezzo == null)
            {
                TempData["ErrorMessage"] = "Mezzo non trovato.";
                return RedirectToPage();
            }

            // Aggiorna lo stato a Disponibile
            var dto = new MezzoUpdateDTO
            {
                IdParcheggioCorrente = mezzo.IdParcheggioCorrente ?? 0,
                LivelloBatteria = mezzo.LivelloBatteria ?? 0,
                Stato = StatoMezzo.Disponibile
            };

            var success = await _apiService.AggiornaMezzoAsync(id, dto);

            if (success)
            {
                TempData["SuccessMessage"] = $"Il mezzo '{mezzo.Matricola}' è tornato disponibile.";
                _logger.LogInformation("Mezzo {Matricola} ripristinato a stato Disponibile dal gestore", mezzo.Matricola);
            }
            else
            {
                TempData["ErrorMessage"] = _apiService.LastError ?? "Errore durante l'aggiornamento del mezzo.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il ripristino del mezzo {Id}", id);
            TempData["ErrorMessage"] = $"Errore imprevisto: {ex.Message}";
        }

        return RedirectToPage();
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