using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;

namespace Mobishare.WebApp.Services;
public interface IMobishareApiService
{
    string? LastError { get; }

    #region AUTENTICAZIONE
    Task<LoginResponseDTO?> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(RegisterDTO request);
    #endregion


    #region UTENTE
    Task<UtenteDTO?> GetUtenteAsync(int id);
    Task<bool> CambiaPasswordAsync(CambiaPswDTO dto);
    //Task<bool> AggiornaProfiloAsync(int id, AggiornaProfiloDTO dto);
    #endregion


    #region RICARICHE
    Task<List<RicaricaResponseDTO>> GetRicaricheUtenteAsync(int utenteId);
    Task<bool> NuovaRicaricaAsync(NuovaRicaricaDTO dto);
    Task<SaldoResponseDTO?> GetSaldoUtenteAsync(int utenteId);
    #endregion


    #region PAGAMENTI
    Task<List<TransazioneResponseDTO>> GetTransazioniByUtenteAsync(int idUtente); // Admin: vede transazioni utente
    Task<List<TransazioneResponseDTO>> GetMieTransazioniAsync();
    Task<TransazioneResponseDTO?> CreaTransazioneManualeAsync(TransazioneCreateDTO dto); // Admin: rimborsi/penali
    Task<bool> AggiornaStatoTransazioneAsync(int idTransazione, StatoPagamento nuovoStato);
    #endregion


    #region CORSE
    Task<List<CorsaResponseDTO>> GetCorseAsync(int? idUtente = null, string? matricolaMezzo = null);
    Task<List<CorsaResponseDTO>> GetStoricoCorseUtenteAsync(int idUtente);
    Task<CorsaResponseDTO?> GetCorsaAsync(int id);
    Task<CorsaResponseDTO?> GetCorsaAttivaAsync();
    Task<CorsaResponseDTO?> IniziaCorsaAsync(AvviaCorsaDTO dto);
    Task<CorsaResponseDTO?> TerminaCorsaAsync(int id, FineCorsaDTO dto);
    #endregion

    #region FEEDBACK
    Task<bool> InviaFeedbackAsync(FeedbackCreateDTO dto);
    Task<List<FeedbackResponseDTO>> GetFeedbackRecentiAsync();
    Task<FeedbackNegativiResponseDTO?> GetFeedbackNegativiAsync();
    Task<FeedbackStatisticheDTO?> GetStatisticheFeedbackAsync();
    Task<List<FeedbackResponseDTO>> GetFeedbackPerUtenteAsync(int idUtente);
    #endregion


    #region MEZZI
    Task<List<MezzoResponseDTO>> GetMezziAsync();
    Task<MezzoResponseDTO?> GetMezzoAsync(int id);
    Task<MezzoResponseDTO?> GetMezzoByMatricolaAsync(string matricola);
    Task<List<MezzoResponseDTO>> GetMezziDisponibiliAsync();
    Task<List<MezzoResponseDTO>> GetMezziPerParcheggioAsync(int idParcheggio);
    Task<MezzoResponseDTO?> CreaMezzoAsync(MezzoCreateDTO dto);
    Task<bool> SpostaMezzoAsync(int idMezzo, MezzoUpdateDTO dto);
    Task<bool> AggiornaMezzoAsync(int id, MezzoUpdateDTO dto);
    Task<bool> SegnalaGuastoAsync(string matricola);
    #endregion


    #region PARCHEGGI
    Task<List<ParcheggioResponseDTO>> GetParcheggiAsync();
    Task<ParcheggioResponseDTO?> GetParcheggioAsync(int id);
    Task<ParcheggioResponseDTO?> CreaParcheggioAsync(ParcheggioCreateDTO dto);
    #endregion


    #region GESTORE
    Task<List<UtenteDTO>> GetTuttiUtentiAsync();
    Task<List<UtenteDTO>> GetUtentiSospesiAsync();
    Task<bool> RiattivaUtenteAsync(int id);
    #endregion


    #region DASHBOARD GESTORE
    Task<DashboardDTO?> GetDashboardAsync(int idUtente);
    #endregion
}

public record ApiSuccessResponse<T>(
    string Messaggio,
    T Dati
);