using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Mobishare.Core.DTOs;

namespace Mobishare.WebApp.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginModel> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public LoginModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<LoginModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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
                foreach (var err in ModelState)
                {
                    _logger.LogWarning("Errore ModelState: {Key} -> {Errors}",
                        err.Key, string.Join(", ", err.Value.Errors.Select(e => e.ErrorMessage)));
                }
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["MobishareApi:BaseUrl"] ?? "https://localhost:7001";

                _logger.LogInformation("Tentativo login per: {Email}", Input.Email);

                var loginRequest = new LoginRequest
                {
                    Email = Input.Email,
                    Password = Input.Password
                };

                var jsonContent = JsonSerializer.Serialize(loginRequest, JsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Invio richiesta di login all'API: {Url}", $"{apiBaseUrl}/api/utenti/login");

                // Chiamata API di login
                var response = await client.PostAsync($"{apiBaseUrl}/api/utenti/login", content);

                _logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
       
                    _logger.LogInformation("Login riuscito. Risposta API ricevuta.");

                    var loginResponse = JsonSerializer.Deserialize<SuccessResponse<LoginResponseDTO>>(responseContent, JsonOptions);
                    _logger.LogInformation("Contenuto ricevuto: {Content}", responseContent);

                    if (loginResponse?.Dati != null)
                    {
                        _logger.LogInformation("Dati utente ricevuti: ID={Id}, Nome={Nome}, Ruolo={Ruolo}",
                            loginResponse.Dati.Id, loginResponse.Dati.Nome, loginResponse.Dati.Ruolo);

                        // IMPORTANTE: Salva TUTTI i dati in Session PRIMA del redirect
                 
                        HttpContext.Session.SetString("JwtToken", loginResponse.Dati.Token);
                        HttpContext.Session.SetInt32("UserId", loginResponse.Dati.Id);
                        HttpContext.Session.SetString("UserName", loginResponse.Dati.Nome);
                        HttpContext.Session.SetString("UserRole", loginResponse.Dati.Ruolo);
                        HttpContext.Session.SetString("UserEmail", Input.Email);
                        HttpContext.Session.SetString("Credito", loginResponse.Dati.Credito.ToString("F2"));
                        HttpContext.Session.SetString("Sospeso", loginResponse.Dati.Sospeso.ToString());

                        // Ricorda sempre l'ultima email (non sensibile, cookie pubblico)
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
                            var cookieOptions = new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddDays(7),
                                HttpOnly = true,
                                Secure = false,
                                SameSite = SameSiteMode.Lax
                            };
                            Response.Cookies.Append("RememberMe", "true", cookieOptions);
                        }

                        // CRITICO: Commit della Session PRIMA del redirect
                        //await HttpContext.Session.CommitAsync();
                        //_logger.LogInformation("Session salvata con successo");

                        // Breve ritardo per assicurare che la session sia persistita
                        //await Task.Delay(100);

                        _logger.LogInformation("Redirect a dashboard per ruolo: {Ruolo}", loginResponse.Dati.Ruolo);

                        // Redirect in base al ruolo
                        if (loginResponse.Dati.Ruolo.Equals("Gestore", StringComparison.OrdinalIgnoreCase))
                        {
                            return RedirectToPage("/DashboardAdmin/Index");
                        }
                        else
                        {
                            return RedirectToPage("/DashboardUtente/Index");
                        }
                    }
                    else
                    {
                        ErrorMessage = "Risposta del server non valida";
                        _logger.LogError("Dati null nella risposta del login");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                         response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Login fallito (401/400): {Error}", errorContent);

                    ErrorMessage = "Email o password non corretti";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Utente sospeso (403): {Email}", Input.Email);

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, JsonOptions);
                        ErrorMessage = errorResponse?.Errore ?? "Account sospeso. Contatta l'assistenza.";
                    }
                    catch
                    {
                        ErrorMessage = "Account sospeso. Contatta l'assistenza.";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Errore server durante login: {StatusCode} - {Content}",
                        response.StatusCode, errorContent);

                    ErrorMessage = $"Si è verificato un errore ({response.StatusCode}). Riprova più tardi.";
                }
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Impossibile connettersi al server. Verifica la tua connessione.";
                _logger.LogError(ex, "Errore di connessione HTTP durante il login per: {Email}", Input.Email);
            }
            catch (JsonException ex)
            {
                ErrorMessage = "Errore nel processare la risposta del server.";
                _logger.LogError(ex, "Errore JSON durante il login per: {Email}", Input.Email);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Si è verificato un errore imprevisto. Riprova più tardi.";
                _logger.LogError(ex, "Errore imprevisto durante il login per: {Email}", Input.Email);
            }

            return Page();
        }
    }
}