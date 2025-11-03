using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;
using System.ComponentModel.DataAnnotations;

namespace Mobishare.WebApp.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly IMobishareApiService _apiService;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(IMobishareApiService apiService, ILogger<ResetPasswordModel> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, MinLength(6)]
            public string NewPassword { get; set; } = string.Empty;

            [Required]
            public string Token { get; set; } = string.Empty;
        }

        //compilamento automatico dei campi da link PasswordDimenticata
        public void OnGet(string? token = null, string? email = null) {
            if (!string.IsNullOrEmpty(token))
                Input.Token = token;
            if (!string.IsNullOrEmpty(email))
                Input.Email = email;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var response = await _apiService.ResetPasswordAsync(Input.Email, Input.NewPassword, Input.Token);
                if (response)
                {
                    SuccessMessage = "Password aggiornata correttamente! Verrai reindirizzato al login...";
                    return Page();
                }

                ErrorMessage = _apiService.LastError ?? "Errore nel reset della password.";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante reset password per {Email}", Input.Email);
                ErrorMessage = "Si è verificato un errore imprevisto.";
                return Page();
            }
        }
    }
}
