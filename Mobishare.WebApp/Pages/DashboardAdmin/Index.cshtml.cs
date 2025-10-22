using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using Mobishare.Core.DTOs; 

namespace Mobishare.WebApp.Pages.DashboardAdmin;

public class IndexModel : PageModel
{
    private readonly IMobishareApiService _apiService;

    public IndexModel(IMobishareApiService apiService)
    {
        _apiService = apiService;
    }

    public DashboardDTO? Dashboard { get; set; }
    public SaldoResponseDTO? Saldo { get; set; }
    public bool IsGestore { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Verifica autenticazione
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Recupera ruolo utente dalla sessione
        var userRole = HttpContext.Session.GetString("UserRole");
        IsGestore = userRole?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) ?? false;

        try
        {
            // Carica dati dashboard
            Dashboard = await _apiService.GetDashboardAsync(userId.Value);

            if (Dashboard == null)
            {
                ErrorMessage = "Impossibile caricare i dati della dashboard.";
                return Page();
            }

            // Se utente normale, carica anche il saldo
            if (!IsGestore)
            {
                Saldo = await _apiService.GetSaldoUtenteAsync(userId.Value);
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "Non hai i permessi per accedere a questa dashboard.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore nel caricamento della dashboard: {ex.Message}";
        }

        return Page();
    }
}