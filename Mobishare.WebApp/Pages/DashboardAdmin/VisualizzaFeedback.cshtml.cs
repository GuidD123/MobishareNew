using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs;

namespace Mobishare.WebApp.Pages.DashboardAdmin;

public class VisualizzaFeedbackModel : PageModel
{
    private readonly IMobishareApiService _apiService;
    private readonly ILogger<VisualizzaFeedbackModel> _logger;

    public VisualizzaFeedbackModel(IMobishareApiService apiService, ILogger<VisualizzaFeedbackModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public FeedbackStatisticheDTO? Statistiche { get; set; }
    public List<FeedbackResponseDTO> FeedbackRecenti { get; set; } = new();
    public FeedbackNegativiResponseDTO? FeedbackNegativi { get; set; }
    public string? ErrorMessage { get; set; }

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

        try
        {
            // Carica statistiche generali
            Statistiche = await _apiService.GetStatisticheFeedbackAsync();

            // Carica ultimi 10 feedback
            FeedbackRecenti = await _apiService.GetFeedbackRecentiAsync();

            // Carica feedback negativi
            FeedbackNegativi = await _apiService.GetFeedbackNegativiAsync();

            _logger.LogInformation("Pagina feedback caricata per admin {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento dei feedback per admin {UserId}", userId);
            ErrorMessage = "Errore nel caricamento dei feedback.";
        }

        return Page();
    }
}