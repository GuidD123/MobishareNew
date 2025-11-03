using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Mobishare.WebApp.Services;

public class MobishareApiService : IMobishareApiService
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<MobishareApiService> _logger;
    public string? LastError { get; private set; }

    public MobishareApiService(HttpClient http, IHttpContextAccessor contextAccessor, ILogger<MobishareApiService> logger)
    {
        //ricevo HttpClient già configurato da Program.cs
        _http = http;
        _contextAccessor = contextAccessor;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
    
    private void AddAuthorizationHeader()
    {
        var token = _contextAccessor.HttpContext?.Session.GetString("JwtToken");

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Nessun token JWT trovato in sessione (MobishareApiService).");
            _http.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            Console.WriteLine($"Token JWT trovato, lunghezza: {token.Length}");
        }
    }

    // Cattura il messaggio di errore dal middleware backend:
    // 1. Il controller lancia un'eccezione custom (es. ElementoNonTrovatoException)
    // 2. ExceptionHandlingMiddleware la serializza in JSON con campo "errore"
    // 3. Questo metodo legge "errore" e lo salva in LastError per la UI
    private async Task SetLastErrorFromResponse(HttpResponseMessage response)
    {
        try
        {
            var errorJson = await response.Content
                .ReadFromJsonAsync<JsonElement>(_jsonOptions);

            LastError = errorJson.TryGetProperty("errore", out var err)
                ? err.GetString()
                : $"Errore HTTP {response.StatusCode}";
        }
        catch
        {
            LastError = $"Errore HTTP {response.StatusCode}";
        }
    }


    #region AUTENTICAZIONE
    public async Task<LoginResponseDTO?> LoginAsync(string email, string password)
    {
        try
        {
            LastError = null; 
            var response = await _http.PostAsJsonAsync("api/utenti/login",
                new LoginRequest { Email = email, Password = password });

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return null;
            }   

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<JsonElement>>(_jsonOptions);

            if (wrapper?.Dati == null)
                return null;

            var dati = wrapper.Dati;
            if (!dati.TryGetProperty("token", out var tokenProp))
                return null;

            return new LoginResponseDTO
            {
                Token = dati.GetProperty("token").GetString() ?? "",
                Id = dati.GetProperty("id").GetInt32(),
                Nome = dati.GetProperty("nome").GetString() ?? "",
                Ruolo = dati.GetProperty("ruolo").GetString() ?? "",
                Credito = dati.GetProperty("credito").GetDecimal(),
                Sospeso = dati.GetProperty("sospeso").GetBoolean()
            };
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public async Task<bool> RegisterAsync(RegisterDTO request)
    {
        try
        {
            LastError = null;
            var response = await _http.PostAsJsonAsync("api/utenti", request);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }
    #endregion

    #region UTENTE
    public async Task<UtenteDTO?> GetUtenteAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/utenti/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<JsonElement>>(_jsonOptions);

            if (wrapper?.Dati == null)
                return null;

            var dati = wrapper.Dati;

            return new UtenteDTO 
            {
                Id = dati.GetProperty("id").GetInt32(),
                Nome = dati.GetProperty("nome").GetString() ?? "",
                Email = dati.GetProperty("email").GetString() ?? "",
                Ruolo = dati.GetProperty("ruolo").GetString() ?? "",
                Credito = dati.GetProperty("credito").GetDecimal(),
                Sospeso = dati.GetProperty("sospeso").GetBoolean()
            };
        }
        catch { return null; }
    }

    public async Task<bool> CambiaPasswordAsync(CambiaPswDTO dto)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PutAsJsonAsync("api/utenti/cambia-password", dto);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, string newPassword, string token)
    {
        try
        {
            var payload = new
            {
                Email = email,
                NewPassword = newPassword,
                Token = token
            };

            var response = await _http.PostAsJsonAsync("/api/utenti/reset-password", payload);
            if (response.IsSuccessStatusCode)
                return true;

            LastError = await response.Content.ReadAsStringAsync();
            return false;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public async Task<bool> AggiornaProfiloAsync(int id, UtenteDTO dto)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();

            // Invia UtenteDTO (già ha Id, Nome, Email, ecc.)
            var response = await _http.PutAsJsonAsync($"api/utenti/{id}", dto);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    #endregion

    #region RICARICHE
    public async Task<List<RicaricaResponseDTO>> GetRicaricheUtenteAsync(int utenteId)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/ricariche/{utenteId}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<RicaricaResponseDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<bool> NuovaRicaricaAsync(NuovaRicaricaDTO dto)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PostAsJsonAsync("api/ricariche", dto);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public async Task<SaldoResponseDTO?> GetSaldoUtenteAsync(int utenteId)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/ricariche/utente/{utenteId}/saldo");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<SaldoResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }
    #endregion

    #region CORSA
    public async Task<List<CorsaResponseDTO>> GetCorseAsync(int? idUtente = null, string? matricolaMezzo = null)
    {
        try
        {
            AddAuthorizationHeader();
            var queryParams = new List<string>();
            if (idUtente.HasValue) queryParams.Add($"idUtente={idUtente.Value}");
            if (!string.IsNullOrEmpty(matricolaMezzo)) queryParams.Add($"matricolaMezzo={matricolaMezzo}");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _http.GetAsync($"api/corse{query}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<CorsaResponseDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<CorsaResponseDTO?> GetCorsaAttivaAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/corse/attiva");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<CorsaResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch
        {
            return null;
        }
    }


    public async Task<List<CorsaResponseDTO>> GetStoricoCorseUtenteAsync(int idUtente)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/corse/utente/{idUtente}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<CorsaResponseDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<CorsaResponseDTO?> GetCorsaAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/corse/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<CorsaResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    //Inizia corsa con gestione errore
    public async Task<CorsaResponseDTO?> IniziaCorsaAsync(AvviaCorsaDTO dto)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PostAsJsonAsync("api/corse/inizia", dto);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return null;
            }

            //var wrapper = await response.Content
            //    .ReadFromJsonAsync<ApiSuccessResponse<CorsaResponseDTO>>(_jsonOptions);

            //return wrapper?.Dati;
            var corsa = await response.Content.ReadFromJsonAsync<CorsaResponseDTO>(_jsonOptions);
            return corsa;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public async Task<CorsaResponseDTO?> TerminaCorsaAsync(int id, FineCorsaDTO dto)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PutAsJsonAsync($"api/corse/{id}", dto);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return null;
            }

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<CorsaResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }
    #endregion

    #region MEZZI
    public async Task<List<MezzoResponseDTO>> GetMezziAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/mezzi");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<MezzoResponseDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }
    public async Task<MezzoResponseDTO?> GetMezzoAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/mezzi/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<MezzoResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }
    public async Task<MezzoResponseDTO?> GetMezzoByMatricolaAsync(string matricola)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/mezzi/matricola/{matricola}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<MezzoResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }
    public async Task<List<MezzoResponseDTO>> GetMezziDisponibiliAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/mezzi/disponibili");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<JsonElement>>(_jsonOptions);

            if (wrapper?.Dati == null)
                return new();

            // Il backend ritorna { totale, mezzi }
            var mezziArray = wrapper.Dati.GetProperty("mezzi");
            return JsonSerializer.Deserialize<List<MezzoResponseDTO>>(mezziArray.GetRawText(), _jsonOptions)
                   ?? new();
        }
        catch { return new(); }
    }
    public async Task<List<MezzoResponseDTO>> GetMezziPerParcheggioAsync(int idParcheggio)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/mezzi/parcheggio/{idParcheggio}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<JsonElement>>(_jsonOptions);

            if (wrapper?.Dati == null)
                return new();

            // Il backend ritorna { parcheggio, totaleMezzi, mezzi, riepilogo }
            var mezziArray = wrapper.Dati.GetProperty("mezzi");
            return JsonSerializer.Deserialize<List<MezzoResponseDTO>>(mezziArray.GetRawText(), _jsonOptions)
                   ?? new();
        }
        catch { return new(); }
    }
    public async Task<MezzoResponseDTO?> CreaMezzoAsync(MezzoCreateDTO dto)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PostAsJsonAsync("api/mezzi", dto);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return null;
            }

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<MezzoResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }
    public async Task<bool> SpostaMezzoAsync(int idMezzo, MezzoUpdateDTO dto)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.PutAsJsonAsync($"api/mezzi/{idMezzo}/stato", dto, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                LastError = $"Errore: {response.StatusCode}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }
    public async Task<bool> AggiornaMezzoAsync(int id, MezzoUpdateDTO dto)
    {
        try
        {
            AddAuthorizationHeader();

            var response = await _http.PutAsJsonAsync($"api/mezzi/{id}/stato", dto);

            if (response.IsSuccessStatusCode)
                return true;

            LastError = await response.Content.ReadAsStringAsync();
            return false;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public async Task<bool> RicaricaMezzoAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.PutAsync($"api/mezzi/{id}/ricarica", null);

            if (response.IsSuccessStatusCode)
                return true;

            LastError = await response.Content.ReadAsStringAsync();
            return false;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public async Task<bool> SegnalaGuastoAsync(string matricola)
    {
        AddAuthorizationHeader();
        var response = await _http.PutAsync($"api/mezzi/matricola/{matricola}/segnala-guasto", null);
        if (response.IsSuccessStatusCode)
            return true;

        LastError = await response.Content.ReadAsStringAsync();
        return false;
    }

    #endregion

    #region FEEDBACK
    public async Task<bool> InviaFeedbackAsync(FeedbackCreateDTO dto)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PostAsJsonAsync("api/feedback", dto);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public async Task<List<FeedbackResponseDTO>> GetFeedbackRecentiAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/feedback/recenti");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<FeedbackResponseDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<FeedbackNegativiResponseDTO?> GetFeedbackNegativiAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/feedback/negativi");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<FeedbackNegativiResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    public async Task<FeedbackStatisticheDTO?> GetStatisticheFeedbackAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/feedback/statistiche");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<FeedbackStatisticheDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    public async Task<List<FeedbackResponseDTO>> GetFeedbackPerUtenteAsync(int idUtente)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/feedback/utente/{idUtente}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<FeedbackResponseDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }
    #endregion

    #region PARCHEGGI
    public async Task<List<ParcheggioResponseDTO>> GetParcheggiAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/parcheggi");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<ParcheggioResponseDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<ParcheggioResponseDTO?> GetParcheggioAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/parcheggi/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<ParcheggioResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    public async Task<ParcheggioResponseDTO?> CreaParcheggioAsync(ParcheggioCreateDTO dto)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PostAsJsonAsync("api/parcheggi", dto);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return null;
            }

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<ParcheggioResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public async Task<bool> AggiornaStatoParcheggioAsync(int id, bool attivo)
    {
        try
        {
            AddAuthorizationHeader();

            var payload = new { Attivo = attivo };
            var response = await _http.PutAsJsonAsync($"api/parcheggi/{id}/stato", payload);

            if (response.IsSuccessStatusCode)
                return true;

            var content = await response.Content.ReadAsStringAsync();
            _logger?.LogWarning("AggiornaStatoParcheggioAsync fallito: {StatusCode} {Content}",
                response.StatusCode, content);

            LastError = $"Errore aggiornamento parcheggio ({response.StatusCode})";
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore durante AggiornaStatoParcheggioAsync (id={Id})", id);
            LastError = "Errore di connessione o server non raggiungibile.";
            return false;
        }
    }
    #endregion

    #region ADMIN
    public async Task<List<UtenteDTO>> GetTuttiUtentiAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/utenti");

            if (!response.IsSuccessStatusCode)
                return new();

            // Questo endpoint ritorna lista diretta, non wrapped
            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<UtenteDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<UtenteDTO>> GetUtentiSospesiAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/utenti/sospesi");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<UtenteDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<bool> RiattivaUtenteAsync(int id)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PutAsync($"api/utenti/{id}/riattiva", null);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public async Task<DashboardDTO?> GetDashboardAsync(int idUtente)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/dashboard?idUtente={idUtente}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<DashboardDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }
    #endregion

    #region TRANSAZIONI/PAGAMENTI
    public async Task<List<TransazioneResponseDTO>> GetTransazioniByUtenteAsync(int idUtente)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"api/pagamenti/utente/{idUtente}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<TransazioneResponseDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<TransazioneResponseDTO>> GetMieTransazioniAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("api/pagamenti/miei");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<TransazioneResponseDTO>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<TransazioneResponseDTO?> CreaTransazioneManualeAsync(TransazioneCreateDTO dto)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PostAsJsonAsync("api/pagamenti", dto);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return null;
            }

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<TransazioneResponseDTO>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    public async Task<bool> AggiornaStatoTransazioneAsync(int idTransazione, StatoPagamento nuovoStato)
    {
        try
        {
            LastError = null;
            AddAuthorizationHeader();
            var response = await _http.PutAsJsonAsync($"api/pagamenti/{idTransazione}", nuovoStato);

            if (!response.IsSuccessStatusCode)
            {
                await SetLastErrorFromResponse(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }


    #endregion
}


