using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mobishare.WebApp.Pages.Account;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        // PULISCI SESSIONE
        HttpContext.Session.Clear();

        // PULISCI COOKIE
        Response.Cookies.Delete(".Mobishare.Session");
        Response.Cookies.Delete("RememberMe");

        TempData["SuccessMessage"] = "Logout effettuato!";

        // VAI ALLA HOME
        return Redirect("/");
    }
}