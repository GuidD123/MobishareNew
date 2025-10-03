using Mobishare.Infrastructure.IoT.Models;
using Mobishare.Infrastructure.IoT.Events;

namespace Mobishare.Infrastructure.IoT.Interfaces
{

    /// <summary>
    /// Servizio per la gestione di scenari di test IoT predefiniti
    /// Idea è permettere di definire scenari di test (batteria scarica, guasto, sblocco ecc) che usano sotto il cofano l'IMqttGatewayEmulatorService.
    /// Mi servirà come demo al'esame, invece di mandare comandi manuali ogni volta, avvio uno scenario già pronto preimpostato 
    /// </summary>
    public interface IIoTScenarioService
    {
        /// <summary>
        /// Avvia uno scenario predefinito di test
        /// </summary>
        Task AvviaScenarioAsync(string nomeScenario, int idParcheggio);

        /// <summary>
        /// Ferma lo scenario in corso
        /// </summary>
        Task FermaScenarioAsync();

        /// <summary>
        /// Ottieni la lista degli scenari disponibili
        /// </summary>
        List<string> GetScenariDisponibili();

        /// <summary>
        /// Verifica se uno scenario è in esecuzione
        /// </summary>
        bool IsScenarioInEsecuzione { get; }

        /// <summary>
        /// Nome dello scenario attualmente in esecuzione
        /// </summary>
        string? ScenarioCorrente { get; }

        /// <summary>
        /// Dettagli dell'ultimo scenario eseguito
        /// </summary>
        string? DettagliScenario { get; }
    }
}
