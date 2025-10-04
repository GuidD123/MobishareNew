using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mobishare.WebApp.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Credito"] = "0.00"; // Per ora hardcoded
        }
    }
}
