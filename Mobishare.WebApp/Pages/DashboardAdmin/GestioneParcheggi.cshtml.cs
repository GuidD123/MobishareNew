using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;

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
}