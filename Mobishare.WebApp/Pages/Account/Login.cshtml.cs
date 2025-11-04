using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.Core.DTOs;
using Mobishare.WebApp.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json;


namespace Mobishare.WebApp.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly IMobishareApiService _apiService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            IMobishareApiService apiService,
            ILogger<LoginModel> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "L'email è obbligatoria")]
            [EmailAddress(ErrorMessage = "Formato email non valido")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "La password è obbligatoria")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Ricordami")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            // Recupera ultima email salvata in cookie (se esiste)
            if (Request.Cookies.TryGetValue("LastEmail", out var savedEmail))
            {
                Input.Email = savedEmail;
            }

            // Controlla se l'utente è già loggato
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                Response.Redirect("/DashboardUtente/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _logger.LogInformation("Tentativo login per: {Email}", Input.Email);

                // Chiamata al service
                var loginResult = await _apiService.LoginAsync(Input.Email, Input.Password);

                if (loginResult == null)
                {
                    // Usa LastError del service
                    ErrorMessage = _apiService.LastError ?? "Email o password non corretti";
                    _logger.LogWarning("Login fallito per {Email}: {Error}", Input.Email, ErrorMessage);
                    return Page();
                }

                // Login riuscito - salva dati in sessione
                _logger.LogInformation("Login riuscito per: {Email} (ID={Id}, Ruolo={Ruolo})",
                    Input.Email, loginResult.Id, loginResult.Ruolo);

                HttpContext.Session.SetString("JwtToken", loginResult.Token);
                HttpContext.Session.SetInt32("UserId", loginResult.Id);
                HttpContext.Session.SetString("UserName", loginResult.Nome);
                HttpContext.Session.SetString("UserRole", loginResult.Ruolo);
                HttpContext.Session.SetString("UserEmail", Input.Email);
                HttpContext.Session.SetString("Credito", loginResult.Credito.ToString("F2"));
                HttpContext.Session.SetString("Sospeso", loginResult.Sospeso.ToString());

                // Cookie per ricordare l'email
                Response.Cookies.Append("LastEmail", Input.Email, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = false,
                    Secure = false,
                    SameSite = SameSiteMode.Lax
                });

                // RememberMe cookie
                if (Input.RememberMe)
                {
                    Response.Cookies.Append("RememberMe", "true", new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(7),
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.Lax
                    });
                }

                // Autenticazione cookie (necessaria per [Authorize])
                var claims = new List<Claim>
{
                new Claim(ClaimTypes.Name, loginResult.Nome),
                new Claim(ClaimTypes.Email, Input.Email),
                new Claim(ClaimTypes.Role, loginResult.Ruolo)
};
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                _logger.LogInformation("Redirect a dashboard per ruolo: {Ruolo}", loginResult.Ruolo);

                // Redirect in base al ruolo
                if (loginResult.Ruolo.Equals("Gestore", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToPage("/DashboardAdmin/Index");
                }
                else
                {
                    return RedirectToPage("/DashboardUtente/Index");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Si è verificato un errore imprevisto. Riprova più tardi.";
                _logger.LogError(ex, "Errore imprevisto durante il login per: {Email}", Input.Email);
                return Page();
            }
        }
    }
}