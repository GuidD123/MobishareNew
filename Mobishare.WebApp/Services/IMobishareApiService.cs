namespace Mobishare.WebApp.Services;

public interface IMobishareApiService
{
    // ====================================
    // AUTENTICAZIONE
    // ====================================
    Task<LoginResponse?> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(RegisterDto request);

    // ====================================
    // UTENTE
    // ====================================
    Task<UtenteDto?> GetUtenteAsync(int id);
    Task<bool> CambiaPasswordAsync(CambiaPswDto dto);
    Task<bool> AggiornaProfiloAsync(int id, AggiornaProfiloDto dto);

    // ====================================
    // RICARICHE
    // ====================================
    Task<List<RicaricaDto>> GetRicaricheUtenteAsync(int utenteId);
    Task<bool> NuovaRicaricaAsync(NuovaRicaricaDto dto);
    Task<SaldoDto?> GetSaldoUtenteAsync(int utenteId);

    // ====================================
    // CORSE
    // ====================================
    Task<List<CorsaDto>> GetCorseAsync(int? idUtente = null, string? matricolaMezzo = null);
    Task<List<CorsaDto>> GetStoricoCorseUtenteAsync(int idUtente);
    Task<CorsaDto?> GetCorsaAsync(int id);
    Task<CorsaDto?> IniziaCorsaAsync(AvviaCorsaDto dto);
    Task<CorsaDto?> TerminaCorsaAsync(int id, FineCorsaDto dto);

    // ====================================
    // MEZZI
    // ====================================
    Task<List<MezzoDto>> GetMezziAsync();
    Task<MezzoDto?> GetMezzoAsync(int id);
    Task<List<MezzoDto>> GetMezziDisponibiliAsync();
    Task<List<MezzoDto>> GetMezziPerParcheggioAsync(int idParcheggio);

    // ====================================
    // PARCHEGGI
    // ====================================
    Task<List<ParcheggioDto>> GetParcheggiAsync();
    Task<ParcheggioDto?> GetParcheggioAsync(int id);

    // ====================================
    // ADMIN (Gestore)
    // ====================================
    Task<List<UtenteDto>> GetTuttiUtentiAsync();
    Task<List<UtenteDto>> GetUtentiSospesiAsync();
    Task<bool> RiattivaUtenteAsync(int id);

    // ====================================
    // DASHBOARD (Gestore)
    // ====================================
    Task<DashboardDto?> GetDashboardAsync(int idUtente);
}

// ====================================
// DTO
// ====================================

public record LoginResponse(
    string Token,
    int Id,
    string Nome,
    string Ruolo,
    decimal Credito,
    bool Sospeso
);

public record RegisterDto(
    string Nome,
    string Cognome,
    string Email,
    string Password
);

public record CambiaPswDto(
    string VecchiaPassword,
    string NuovaPassword
);

public record AggiornaProfiloDto(
    string Nome,
    string Password
);

public record UtenteDto(
    int Id,
    string Nome,
    string Email,
    string Ruolo,
    decimal Credito,
    bool Sospeso
);

public record NuovaRicaricaDto(
    int IdUtente,
    decimal ImportoRicarica,
    string TipoRicarica = "Carta"
);

public record RicaricaDto(
    int Id,
    decimal ImportoRicarica,
    DateTime DataRicarica,
    string Tipo,
    string Stato
);

public record SaldoDto(
    decimal CreditoAttuale,
    bool UtenteAttivo,
    decimal TotaleRicariche,
    decimal RicaricheInSospeso,
    decimal TotaleSpese,
    DateTime? UltimaRicarica
);

public record AvviaCorsaDto(
    string MatricolaMezzo,
    int IdParcheggioPrelievo
);

public record FineCorsaDto(
    DateTime DataOraFineCorsa,
    int IdParcheggioRilascio,
    bool SegnalazioneProblema
);

public record CorsaDto(
    int Id,
    int IdUtente,
    string MatricolaMezzo,
    int IdParcheggioPrelievo,
    int? IdParcheggioRilascio,
    DateTime DataOraInizio,
    DateTime? DataOraFine,
    decimal? CostoFinale
);

public record MezzoDto(
    int Id,
    string Matricola,
    string Tipo,
    string Stato,
    int? LivelloBatteria,
    int? IdParcheggioCorrente,
    string? NomeParcheggio
);

public record ParcheggioDto(
    int Id,
    string Nome,
    string Zona,
    string Indirizzo,
    int Capienza,
    bool Attivo,
    List<MezzoDto> Mezzi
);

public record DashboardDto(
    int NumeroCorseTotali,
    int CorseOggi,
    int CorseUltimaSettimana,
    int MezziDisponibili,
    int MezziInUso,
    int MezziGuasti,
    int UtentiSospesi,
    decimal CreditoTotaleSistema,
    string? Messaggio
);

public record ApiSuccessResponse<T>(
    string Messaggio,
    T Dati
);