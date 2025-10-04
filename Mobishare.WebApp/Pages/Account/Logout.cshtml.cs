using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mobishare.WebApp.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Pulisci la sessione
            HttpContext.Session.Clear();

            // Redirect al login
            return RedirectToPage("/Account/Login");
        }
    }
}