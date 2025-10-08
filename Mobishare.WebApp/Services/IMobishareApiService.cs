using Mobishare.Core.DTOs; 

namespace Mobishare.WebApp.Services;
public interface IMobishareApiService
{
    // ====================================
    // AUTENTICAZIONE
    // ====================================
    Task<LoginResponseDTO?> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(RegisterDTO request);

    // ====================================
    // UTENTE
    // ====================================
    Task<UtenteDTO?> GetUtenteAsync(int id);
    Task<bool> CambiaPasswordAsync(CambiaPswDTO dto);
    //Task<bool> AggiornaProfiloAsync(int id, AggiornaProfiloDTO dto);

    // ====================================
    // RICARICHE
    // ====================================
    Task<List<RicaricaResponseDTO>> GetRicaricheUtenteAsync(int utenteId);
    Task<bool> NuovaRicaricaAsync(NuovaRicaricaDTO dto);
    Task<SaldoResponseDTO?> GetSaldoUtenteAsync(int utenteId);

    // ====================================
    // CORSE
    // ====================================
    Task<List<CorsaResponseDTO>> GetCorseAsync(int? idUtente = null, string? matricolaMezzo = null);
    Task<List<CorsaResponseDTO>> GetStoricoCorseUtenteAsync(int idUtente);
    Task<CorsaResponseDTO?> GetCorsaAsync(int id);
    Task<CorsaResponseDTO?> IniziaCorsaAsync(AvviaCorsaDTO dto);
    Task<CorsaResponseDTO?> TerminaCorsaAsync(int id, FineCorsaDTO dto);

    // ====================================
    // MEZZI
    // ====================================
    Task<List<MezzoResponseDTO>> GetMezziAsync();
    Task<MezzoResponseDTO?> GetMezzoAsync(int id);
    Task<MezzoResponseDTO?> GetMezzoByMatricolaAsync(string matricola);
    Task<List<MezzoResponseDTO>> GetMezziDisponibiliAsync();
    Task<List<MezzoResponseDTO>> GetMezziPerParcheggioAsync(int idParcheggio);

    // ====================================
    // PARCHEGGI
    // ====================================
    Task<List<ParcheggioResponseDTO>> GetParcheggiAsync();
    Task<ParcheggioResponseDTO?> GetParcheggioAsync(int id);

    // ====================================
    // ADMIN (Gestore)
    // ====================================
    Task<List<UtenteDTO>> GetTuttiUtentiAsync();
    Task<List<UtenteDTO>> GetUtentiSospesiAsync();
    Task<bool> RiattivaUtenteAsync(int id);

    // ====================================
    // DASHBOARD (Gestore)
    // ====================================
    Task<DashboardDTO?> GetDashboardAsync(int idUtente);
}

public record ApiSuccessResponse<T>(
    string Messaggio,
    T Dati
);