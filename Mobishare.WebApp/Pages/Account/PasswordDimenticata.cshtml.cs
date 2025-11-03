using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Mobishare.WebApp.Pages.Account
{
    public class PasswordDimenticataModel : PageModel
    {
        [BindProperty]
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Message { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // In futuro: chiamata API tipo /api/utenti/request-password-reset
            // Qui simuliamo generando token finto
            var fakeToken = Guid.NewGuid().ToString("N");

            // Costruisci link cliccabile verso ResetPassword
            var linkReset = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { token = fakeToken, email = Email },
                protocol: Request.Scheme);

            // Mostra link (in reale sarebbe inviato via email)
            Message = $"<strong>Link di reset (solo test):</strong> <a href='{linkReset}'>{linkReset}</a>";

            await Task.CompletedTask;
            return Page();
        }
    }
}
