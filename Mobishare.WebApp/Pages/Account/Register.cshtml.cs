using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Mobishare.WebApp.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterModel> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public RegisterModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<RegisterModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Il nome è obbligatorio")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "Il nome deve essere tra 3 e 50 caratteri")]
            [Display(Name = "Nome")]
            public string Nome { get; set; } = string.Empty;

            [Required(ErrorMessage = "Il cognome è obbligatorio")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "Il cognome deve essere tra 3 e 50 caratteri")]
            [Display(Name = "Cognome")]
            public string Cognome { get; set; } = string.Empty;

            [Required(ErrorMessage = "L'email è obbligatoria")]
            [EmailAddress(ErrorMessage = "Formato email non valido")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "La password è obbligatoria")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "La password deve essere di almeno 8 caratteri")]
            [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
                ErrorMessage = "La password deve contenere almeno una lettera maiuscola e un numero")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "La conferma password è obbligatoria")]
            [DataType(DataType.Password)]
            [Display(Name = "Conferma Password")]
            [Compare("Password", ErrorMessage = "Le password non coincidono")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Devi accettare i termini e condizioni")]
            [Range(typeof(bool), "true", "true", ErrorMessage = "Devi accettare i termini e condizioni")]
            [Display(Name = "Accetto i termini")]
            public bool AcceptTerms { get; set; }
        }

        public void OnGet()
        {
            // Controlla se l'utente è già loggato
            if (HttpContext.Session.GetString("Token") != null)
            {
                Response.Redirect("/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                //var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
                var apiBaseUrl = _configuration["MobishareApi:BaseUrl"] ?? "https://localhost:7001";

                // Prepara il payload per la richiesta di registrazione
                var registerRequest = new
                {
                    email = Input.Email,
                    nome = Input.Nome,
                    cognome = Input.Cognome,
                    password = Input.Password
                };

                var jsonContent = JsonSerializer.Serialize(registerRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Chiamata API di registrazione
                var response = await client.PostAsync($"{apiBaseUrl}/api/utenti", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var registerResponse = JsonSerializer.Deserialize<RegisterResponse>(responseContent, JsonOptions);

                    _logger.LogInformation("Registrazione completata con successo per: {Email}", Input.Email);

                    SuccessMessage = "Registrazione completata! Ora puoi effettuare il login.";

                    // Pulisci il form
                    ModelState.Clear();
                    Input = new InputModel();

                    // Opzionale: Redirect automatico al login dopo 3 secondi
                    TempData["SuccessMessage"] = "Registrazione completata! Effettua il login per continuare.";

                    return Page();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict ||
                         response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, JsonOptions);

                    if (errorResponse?.Errore?.Contains("esiste già") == true)
                    {
                        ErrorMessage = "Un utente con questa email è già registrato. Prova ad effettuare il login.";
                    }
                    else
                    {
                        ErrorMessage = errorResponse?.Errore ?? "Dati non validi. Controlla i campi e riprova.";
                    }

                    _logger.LogWarning("Tentativo di registrazione fallito per: {Email}. Motivo: {Error}",
                        Input.Email, ErrorMessage);
                }
                else
                {
                    ErrorMessage = "Si è verificato un errore durante la registrazione. Riprova più tardi.";
                    _logger.LogError("Errore durante la registrazione. Status Code: {StatusCode}", response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Impossibile connettersi al server. Verifica la tua connessione e riprova.";
                _logger.LogError(ex, "Errore di connessione durante la registrazione per: {Email}", Input.Email);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Si è verificato un errore imprevisto. Riprova più tardi.";
                _logger.LogError(ex, "Errore imprevisto durante la registrazione per: {Email}", Input.Email);
            }

            return Page();
        }

        // Classi per la deserializzazione della risposta
        private class RegisterResponse
        {
            public string? Messaggio { get; set; }
            public RegisterData? Dati { get; set; }
        }

        private class RegisterData
        {
            public int Id { get; set; }
            public string Nome { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        private class ErrorResponse
        {
            public string? Errore { get; set; }
            public string? Campo { get; set; }
            public string? TraceId { get; set; }
        }
    }
}