using Mobishare.WebApp.Services;
using System.Net.Http.Json;
using System.Text.Json;

public class MobishareApiService : IMobishareApiService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;

    public MobishareApiService(HttpClient http)
    {
        _http = http;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // AUTENTICAZIONE
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", new { email, password });
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
            return result;
        }
        catch { return null; }
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/register", request);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // UTENTE
    public async Task<UtenteDto?> GetUtenteAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<UtenteDto>($"/api/utenti/{id}", _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<bool> RicaricaCreditoAsync(int utenteId, decimal importo)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/ricariche", new { idUtente = utenteId, importoRicarica = importo });
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<RicaricaDto>> GetRicaricheUtenteAsync(int utenteId)
    {
        try
        {
            var response = await _http.GetAsync($"/api/ricariche/{utenteId}");
            if (!response.IsSuccessStatusCode) return new();

            var wrapper = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<List<RicaricaDto>>>(_jsonOptions);
            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    // CORSE
    public async Task<List<CorsaDto>> GetCorseUtenteAsync(int utenteId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CorsaDto>>($"/api/corse/utente/{utenteId}", _jsonOptions) ?? new();
        }
        catch { return new(); }
    }

    public async Task<CorsaDto?> GetCorsaCorrenteAsync(int utenteId)
    {
        try
        {
            return await _http.GetFromJsonAsync<CorsaDto>($"/api/corse/utente/{utenteId}/corrente", _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<CorsaDto?> IniziaCorsaAsync(int utenteId, int mezzoId)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/corse", new { idUtente = utenteId, idMezzo = mezzoId });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CorsaDto>(_jsonOptions);
        }
        catch { return null; }
    }

    public async Task<bool> TerminaCorsaAsync(int corsaId, decimal latitudine, decimal longitudine)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/corse/{corsaId}/termina", new { latitudine, longitudine });
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // MEZZI
    public async Task<List<MezzoDto>> GetMezziDisponibiliAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<MezzoDto>>("/api/mezzi?stato=disponibile", _jsonOptions) ?? new();
        }
        catch { return new(); }
    }

    public async Task<MezzoDto?> GetMezzoAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<MezzoDto>($"/api/mezzi/{id}", _jsonOptions);
        }
        catch { return null; }
    }

    // PARCHEGGI
    public async Task<List<ParcheggioDto>> GetParcheggiAsync()
    {
        try
        {
            var response = await _http.GetAsync("/api/parcheggi");
            if (!response.IsSuccessStatusCode) return new();

            var wrapper = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<List<ParcheggioDto>>>(_jsonOptions);
            return wrapper?.Dati ?? new();
        }
        catch { return new(); }
    }

    public async Task<ParcheggioDto?> GetParcheggioAsync(int id)
    {
        try
        {
            var response = await _http.GetAsync($"/api/parcheggi/{id}");
            if (!response.IsSuccessStatusCode) return null;

            var wrapper = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<ParcheggioDto>>(_jsonOptions);
            return wrapper?.Dati;
        }
        catch { return null; }
    }

    // ADMIN
    public async Task<List<UtenteDto>> GetTuttiUtentiAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UtenteDto>>("/api/utenti", _jsonOptions) ?? new();
        }
        catch { return new(); }
    }
}

// Wrapper per risposte API del backend
internal record ApiSuccessResponse<T>(string Messaggio, T Dati);