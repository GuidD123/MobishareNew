using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mobishare.WebApp.Services;

namespace Mobishare.WebApp.Pages.DashboardUtente;

public class IndexModel : PageModel
{
    private readonly IMobishareApiService _apiService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IMobishareApiService apiService, ILogger<IndexModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    // Dati utente
    public string NomeUtente { get; set; } = string.Empty;
    public decimal Credito { get; set; }
    public bool Sospeso { get; set; }

    // Statistiche
    public int TotaleCorseCompletate { get; set; }
    public decimal SpesaTotale { get; set; }

    // Corsa attiva
    public bool HasCorsaInCorso { get; set; }
    public CorsaAttivaDto? CorsaAttiva { get; set; }

    // Messaggi
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Verifica autenticazione
        var userId = HttpContext.Session.GetInt32("UserId");
        var token = HttpContext.Session.GetString("JwtToken");

        _logger.LogInformation("OnGetAsync - UserId: {UserId}, Token presente: {HasToken}",
            userId, !string.IsNullOrEmpty(token));

        if (userId == null || string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Tentativo accesso dashboard senza autenticazione completa. UserId: {UserId}, Token: {HasToken}",
                userId, !string.IsNullOrEmpty(token));
            return RedirectToPage("/Account/Login");
        }

        // Carica dati dalla session (fallback)
        NomeUtente = HttpContext.Session.GetString("UserName") ?? "Utente";

        var creditoStr = HttpContext.Session.GetString("Credito");
        if (decimal.TryParse(creditoStr, out var creditoSession))
        {
            Credito = creditoSession;
        }

        var sospesoStr = HttpContext.Session.GetString("Sospeso");
        if (bool.TryParse(sospesoStr, out var sospesoSession))
        {
            Sospeso = sospesoSession;
        }

        try
        {
            // Carica dati utente aggiornati dall'API
            _logger.LogInformation("Caricamento dati utente {UserId} dall'API", userId.Value);

            var utente = await _apiService.GetUtenteAsync(userId.Value);
            if (utente != null)
            {
                NomeUtente = utente.Nome;
                Credito = utente.Credito;
                Sospeso = utente.Sospeso;

                // Aggiorna session con dati freschi
                HttpContext.Session.SetString("UserName", utente.Nome);
                HttpContext.Session.SetString("Credito", utente.Credito.ToString("F2"));
                HttpContext.Session.SetString("Sospeso", utente.Sospeso.ToString());

                _logger.LogInformation("Dati utente caricati: Nome={Nome}, Credito={Credito}, Sospeso={Sospeso}",
                    utente.Nome, utente.Credito, utente.Sospeso);
            }
            else
            {
                _logger.LogWarning("GetUtenteAsync ha restituito null per userId {UserId}", userId.Value);
            }


            //caricamento saldo utente aggiornato da API 
            _logger.LogInformation("Caricamento saldo utente {UserId}", userId.Value);

            var saldo = await _apiService.GetSaldoUtenteAsync(userId.Value);
            if (saldo != null)
            {
                Credito = saldo.CreditoAttuale;
                Sospeso = !saldo.UtenteAttivo;
                SpesaTotale = saldo.TotaleSpese;

                _logger.LogInformation("Saldo utente: Credito={Credito}, TotaleSpese={Spese}, UtenteAttivo={Attivo}",
                    saldo.CreditoAttuale, saldo.TotaleSpese, saldo.UtenteAttivo);
            }
            else
            {
                _logger.LogWarning("GetSaldoUtenteAsync ha restituito null per userId {UserId}", userId.Value);
            }

            // Carica storico corse per statistiche
            _logger.LogInformation("Caricamento storico corse per utente {UserId}", userId.Value);

            var corse = await _apiService.GetStoricoCorseUtenteAsync(userId.Value);

            _logger.LogInformation("Storico corse caricato: {Count} corse totali", corse.Count);

            TotaleCorseCompletate = corse.Count(c => c.DataOraFine.HasValue);
            SpesaTotale = corse
                .Where(c => c.CostoFinale.HasValue)
                .Sum(c => c.CostoFinale!.Value);

            // Verifica corsa in corso
            var corsaInCorso = corse.FirstOrDefault(c => c.DataOraFine == null);
            if (corsaInCorso != null)
            {
                _logger.LogInformation("Trovata corsa in corso: ID={IdCorsa}, Matricola={Matricola}",
                    corsaInCorso.Id, corsaInCorso.MatricolaMezzo);

                HasCorsaInCorso = true;

                try
                {
                    var mezzo = await _apiService.GetMezzoByMatricolaAsync(corsaInCorso.MatricolaMezzo);

                    CorsaAttiva = new CorsaAttivaDto
                    {
                        IdCorsa = corsaInCorso.Id,
                        MatricolaMezzo = corsaInCorso.MatricolaMezzo,
                        TipoMezzo = mezzo?.Tipo ?? corsaInCorso.TipoMezzo ?? "N/A",
                        DataOraInizio = corsaInCorso.DataOraInizio
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante il caricamento del mezzo per la corsa {IdCorsa}", corsaInCorso.Id);

                    // Fallback: usa i dati della corsa
                    CorsaAttiva = new CorsaAttivaDto
                    {
                        IdCorsa = corsaInCorso.Id,
                        MatricolaMezzo = corsaInCorso.MatricolaMezzo,
                        TipoMezzo = corsaInCorso.TipoMezzo ?? "N/A",
                        DataOraInizio = corsaInCorso.DataOraInizio
                    };
                }
            }

            // Messaggio da TempData
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }

            _logger.LogInformation("Dashboard caricata con successo per utente {UserId}", userId.Value);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Errore di connessione durante il caricamento della dashboard per utente {UserId}", userId);
            ErrorMessage = "Errore di connessione al server. Verifica la tua connessione e riprova.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore generico durante il caricamento dashboard per utente {UserId}", userId);
            ErrorMessage = "Errore nel caricamento della dashboard. Alcuni dati potrebbero non essere aggiornati.";
        }

        return Page();
    }
}

// DTO per corsa attiva
public class CorsaAttivaDto
{
    public int IdCorsa { get; set; }
    public string MatricolaMezzo { get; set; } = string.Empty;
    public string TipoMezzo { get; set; } = string.Empty;
    public DateTime DataOraInizio { get; set; }
}