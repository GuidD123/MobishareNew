using Mobishare.Core.Enums;

namespace Mobishare.IoT.Gateway.Interfaces
{
    public interface IMqttGatewayEmulatorService
    {
        Task AvviaAsync();
        Task FermaAsync();
        /// <summary>
        /// Avvia l'emulatore del Gateway IoT per un parcheggio specifico
        /// </summary>
        Task StartAsync(int idParcheggio, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ferma l'emulatore
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se l'emulatore è in esecuzione
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// ID del parcheggio gestito da questo emulatore
        /// </summary>
        int IdParcheggio { get; }

        //GESTIONE MEZZI EMULATI

        /// <summary>
        /// Aggiunge un mezzo da emulare
        /// </summary>
        Task AggiungiMezzoEmulato(string idMezzo, string matricola, TipoMezzo tipo, StatoMezzo statoIniziale, int? livelloBatteria);

        /// <summary>
        /// Rimuove un mezzo dall'emulazione
        /// </summary>
        Task RimuoviMezzoEmulato(string idMezzo);

        /// <summary>
        /// Ottieni la lista dei mezzi attualmente emulati
        /// </summary>
        List<string> GetMezziEmulati();


        //COMPORTAMENTI
        
        /// <summary>
        /// Simula un cambio di stato per un mezzo
        /// </summary>
        Task SimulaCambioStatoAsync(string idMezzo, StatoMezzo nuovoStato);

        /// <summary>
        /// Simula variazione del livello batteria (per mezzi elettrici)
        /// </summary>
        Task SimulaVariazioneBatteriaAsync(string idMezzo, int nuovoLivello);

        /// <summary>
        /// Avvia la simulazione automatica di eventi casuali
        /// </summary>
        Task AvviaSimulazioneAutomaticaAsync(TimeSpan intervallo);

        /// <summary>
        /// Ferma la simulazione automatica
        /// </summary>
        Task FermaSimulazioneAutomaticaAsync();
    }
}
