namespace Mobishare.WebApp.Services;
public interface IMobishareApiService
{
    // AUTENTICAZIONE
    Task<LoginResponse?> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(RegisterRequest request);

    // UTENTE
    Task<UtenteDto?> GetUtenteAsync(int id);
    Task<bool> RicaricaCreditoAsync(int utenteId, decimal importo);
    Task<List<RicaricaDto>> GetRicaricheUtenteAsync(int utenteId);

    // CORSE
    Task<List<CorsaDto>> GetCorseUtenteAsync(int utenteId);
    Task<CorsaDto?> GetCorsaCorrenteAsync(int utenteId);
    Task<CorsaDto?> IniziaCorsaAsync(int utenteId, int mezzoId);
    Task<bool> TerminaCorsaAsync(int corsaId, decimal latitudine, decimal longitudine);

    // MEZZI
    Task<List<MezzoDto>> GetMezziDisponibiliAsync();
    Task<MezzoDto?> GetMezzoAsync(int id);

    // PARCHEGGI
    Task<List<ParcheggioDto>> GetParcheggiAsync();
    Task<ParcheggioDto?> GetParcheggioAsync(int id);

    // ADMIN (facoltativo)
    Task<List<UtenteDto>> GetTuttiUtentiAsync();
}

// DTO basati sul TUO backend
public record LoginResponse(int Id, string Email, string Nome, string Cognome, string Ruolo);

public record RegisterRequest(string Nome, string Cognome, string Email, string Password);

public record UtenteDto(
    int Id,
    string Nome,
    string Cognome,
    string Email,
    decimal Credito,
    decimal DebitoResiduo,
    bool Sospeso
);

public record CorsaDto(
    int Id,
    int IdUtente,
    int IdMezzo,
    DateTime DataInizio,
    DateTime? DataFine,
    decimal? Latitudine,
    decimal? Longitudine,
    decimal? CostoTotale,
    string Stato
);

public record MezzoDto(
    int Id,
    string Targa,
    string TipoMezzo,
    decimal TariffaOraria,
    string Stato,
    int? IdParcheggio,
    int? BatteriaPercentuale
);

public record ParcheggioDto(
    int Id,
    string Nome,
    string Indirizzo,
    decimal Latitudine,
    decimal Longitudine,
    int Capienza,
    int PostiDisponibili
);

public record RicaricaDto(
    int Id,
    decimal ImportoRicarica,
    DateTime DataRicarica,
    string Tipo,
    string Stato
);