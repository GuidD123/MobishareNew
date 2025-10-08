using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Helpers;

namespace Mobishare.WebApp.Pages.Account
{
    /// <summary>
    /// PageModel per Logout - NO VISTA (.cshtml)
    /// Viene chiamato tramite form/link e fa solo redirect
    /// </summary>
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        // Gestisce richieste GET: /Account/Logout
        public IActionResult OnGet()
        {
            return ExecuteLogout();
        }

        // Gestisce richieste POST: /Account/Logout (PIÙ SICURO)
        public IActionResult OnPost()
        {
            return ExecuteLogout();
        }

        // Logica centralizzata
        private IActionResult ExecuteLogout()
        {
            var userEmail = HttpContext.Session.GetUserEmail();
            var userName = HttpContext.Session.GetUserName();

            // Pulisce sessione
            HttpContext.Session.ClearUserData();

            // Pulisce cookie RememberMe
            if (Request.Cookies["RememberMe"] != null)
            {
                Response.Cookies.Delete("RememberMe");
            }

            _logger.LogInformation("Logout - Utente: {Name} ({Email})",
                userName ?? "Unknown", userEmail ?? "Unknown");

            TempData["SuccessMessage"] = "Logout effettuato con successo!";

            return RedirectToPage("./Login");
        }
    }
}