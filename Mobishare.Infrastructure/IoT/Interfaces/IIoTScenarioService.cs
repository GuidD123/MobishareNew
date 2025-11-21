/*namespace Mobishare.Infrastructure.IoT.Interfaces
{
    /// <summary>
    /// Servizio per la gestione di scenari di test IoT predefiniti.
    /// Permette di eseguire sequenze automatiche di comandi sull'emulatore
    /// per demo e testing senza dover inviare comandi manuali.
    /// </summary>
    public interface IIoTScenarioService
    {
        /// <summary>
        /// Avvia uno scenario predefinito di test
        /// </summary>
        /// <param name="nomeScenario">Nome dello scenario (es. "BatteriaScarica")</param>
        /// <param name="idParcheggio">ID del parcheggio su cui eseguire lo scenario</param>
        Task AvviaScenarioAsync(string nomeScenario, int idParcheggio);

        /// <summary>
        /// Ferma lo scenario attualmente in corso
        /// </summary>
        Task FermaScenarioAsync();

        /// <summary>
        /// Ottieni la lista degli scenari disponibili
        /// </summary>
        List<string> GetScenariDisponibili();

        /// <summary>
        /// Indica se uno scenario è attualmente in esecuzione
        /// </summary>
        bool IsScenarioInEsecuzione { get; }

        /// <summary>
        /// Nome dello scenario attualmente in esecuzione (null se nessuno)
        /// </summary>
        string? ScenarioCorrente { get; }

        /// <summary>
        /// Dettagli testuali dell'ultimo scenario eseguito o in corso
        /// </summary>
        string? DettagliScenario { get; }
    }
}*/