using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mobishare.WebApp.Services;

public class MobishareApiService : IMobishareApiService
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;

    public MobishareApiService(HttpClient http, IHttpContextAccessor contextAccessor)
    {
        _http = http;
        _contextAccessor = contextAccessor;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private void AddAuthorizationHeader()
    {
        var token = _contextAccessor.HttpContext?.Session.GetString("JwtToken");
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // ====================================
    // AUTENTICAZIONE
    // ====================================

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/utenti/login",
                new { email, password });

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<JsonElement>>(_jsonOptions);

            if (wrapper?.Dati == null)
                return null;

            var dati = wrapper.Dati;

            return new LoginResponse(
                Token: dati.GetProperty("token").GetString() ?? "",
                Id: dati.GetProperty("id").GetInt32(),
                Nome: dati.GetProperty("nome").GetString() ?? "",
                Ruolo: dati.GetProperty("ruolo").GetString() ?? "",
                Credito: dati.GetProperty("credito").GetDecimal(),
                Sospeso: dati.GetProperty("sospeso").GetBoolean()
            );
        }
        catch { return null; }
    }

    public async Task<bool> RegisterAsync(RegisterDto request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/utenti", request);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ====================================
    // UTENTE
    // ====================================

    public async Task<UtenteDto?> GetUtenteAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"/api/utenti/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<JsonElement>>(_jsonOptions);

            if (wrapper?.Dati == null)
                return null;

            var dati = wrapper.Dati;

            return new UtenteDto(
                Id: dati.GetProperty("id").GetInt32(),
                Nome: dati.GetProperty("nome").GetString() ?? "",
                Email: dati.GetProperty("email").GetString() ?? "",
                Ruolo: dati.GetProperty("ruolo").GetString() ?? "",
                Credito: dati.GetProperty("credito").GetDecimal(),
                Sospeso: dati.GetProperty("sospeso").GetBoolean()
            );
        }
        catch { return null; }
    }

    public async Task<bool> CambiaPasswordAsync(CambiaPswDto dto)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.PutAsJsonAsync("/api/utenti/cambia-password", dto);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> AggiornaProfiloAsync(int id, AggiornaProfiloDto dto)
    {
        try
        {
            AddAuthorizationHeader();
            var body = new { id, nome = dto.Nome, password = dto.Password };
            var response = await _http.PutAsJsonAsync($"/api/utenti/{id}", body);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ====================================
    // RICARICHE
    // ====================================

    public async Task<List<RicaricaDto>> GetRicaricheUtenteAsync(int utenteId)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"/api/ricariche/{utenteId}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<RicaricaDto>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<bool> NuovaRicaricaAsync(NuovaRicaricaDto dto)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.PostAsJsonAsync("/api/ricariche", dto);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<SaldoDto?> GetSaldoUtenteAsync(int utenteId)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"/api/ricariche/utente/{utenteId}/saldo");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<SaldoDto>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    // ====================================
    // CORSE
    // ====================================

    public async Task<List<CorsaDto>> GetCorseAsync(int? idUtente = null, string? matricolaMezzo = null)
    {
        try
        {
            AddAuthorizationHeader();
            var queryParams = new List<string>();
            if (idUtente.HasValue) queryParams.Add($"idUtente={idUtente.Value}");
            if (!string.IsNullOrEmpty(matricolaMezzo)) queryParams.Add($"matricolaMezzo={matricolaMezzo}");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _http.GetAsync($"/api/corse{query}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<CorsaDto>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<CorsaDto>> GetStoricoCorseUtenteAsync(int idUtente)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"/api/corse/utente/{idUtente}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<CorsaDto>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<CorsaDto?> GetCorsaAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"/api/corse/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<CorsaDto>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    public async Task<CorsaDto?> IniziaCorsaAsync(AvviaCorsaDto dto)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.PostAsJsonAsync("/api/corse/inizia", dto);

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<CorsaDto>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    public async Task<CorsaDto?> TerminaCorsaAsync(int id, FineCorsaDto dto)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.PutAsJsonAsync($"/api/corse/{id}", dto);

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<CorsaDto>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    // ====================================
    // MEZZI
    // ====================================

    public async Task<List<MezzoDto>> GetMezziAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("/api/mezzi");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<MezzoDto>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<MezzoDto?> GetMezzoAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"/api/mezzi/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<MezzoDto>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    public async Task<List<MezzoDto>> GetMezziDisponibiliAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("/api/mezzi/disponibili");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<JsonElement>>(_jsonOptions);

            if (wrapper?.Dati == null)
                return new();

            // Il backend ritorna { totale, mezzi }
            var mezziArray = wrapper.Dati.GetProperty("mezzi");
            return JsonSerializer.Deserialize<List<MezzoDto>>(mezziArray.GetRawText(), _jsonOptions)
                   ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<MezzoDto>> GetMezziPerParcheggioAsync(int idParcheggio)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"/api/mezzi/parcheggio/{idParcheggio}");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<JsonElement>>(_jsonOptions);

            if (wrapper?.Dati == null)
                return new();

            // Il backend ritorna { parcheggio, totaleMezzi, mezzi, riepilogo }
            var mezziArray = wrapper.Dati.GetProperty("mezzi");
            return JsonSerializer.Deserialize<List<MezzoDto>>(mezziArray.GetRawText(), _jsonOptions)
                   ?? new();
        }
        catch { return new(); }
    }

    // ====================================
    // PARCHEGGI
    // ====================================

    public async Task<List<ParcheggioDto>> GetParcheggiAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("/api/parcheggi");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<ParcheggioDto>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<ParcheggioDto?> GetParcheggioAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"/api/parcheggi/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<ParcheggioDto>>(_jsonOptions);

            return wrapper?.Dati;
        }
        catch { return null; }
    }

    // ====================================
    // ADMIN (Gestore)
    // ====================================

    public async Task<List<UtenteDto>> GetTuttiUtentiAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("/api/utenti");

            if (!response.IsSuccessStatusCode)
                return new();

            // Questo endpoint ritorna lista diretta, non wrapped
            return await response.Content
                .ReadFromJsonAsync<List<UtenteDto>>(_jsonOptions) ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<UtenteDto>> GetUtentiSospesiAsync()
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync("/api/utenti/sospesi");

            if (!response.IsSuccessStatusCode)
                return new();

            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiSuccessResponse<List<UtenteDto>>>(_jsonOptions);

            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<bool> RiattivaUtenteAsync(int id)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.PostAsync($"/api/utenti/{id}/riattiva", null);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<DashboardDto?> GetDashboardAsync(int idUtente)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _http.GetAsync($"/api/dashboard?idUtente={idUtente}");

            if (!response.IsSuccessStatusCode)
                return null;

            // Questo endpoint ritorna direttamente l'oggetto, non wrapped
            return await response.Content
                .ReadFromJsonAsync<DashboardDto>(_jsonOptions);
        }
        catch { return null; }
    }
}