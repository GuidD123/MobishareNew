using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Models;
using Mobishare.Infrastructure.IoT.Events;

namespace Mobishare.Infrastructure.IoT.Interfaces
{
    /// <summary>
    /// Servizio principale per la comunicazione MQTT con il sottosistema IoT
    /// Utilizzato dal Backend per inviare comandi e ricevere status dai Gateway IoT
    /// </summary>
    public interface IMqttIoTService
    {
        /// <summary>
        /// Avvia il servizio MQTT e si connette al broker
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Ferma il servizio MQTT
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se il client è connesso al broker
        /// </summary>
        bool IsConnected { get; }

        // === INVIO COMANDI (Backend → Gateway IoT) ===

        /// <summary>
        /// Invia un comando a un mezzo specifico
        /// </summary>
        Task InviaComandoMezzoAsync(int idParcheggio, string idMezzo, ComandoMezzoMessage comando);

        /// <summary>
        /// Sblocca un mezzo per iniziare una corsa
        /// </summary>
        Task SbloccaMezzoAsync(int idParcheggio, string idMezzo, string utenteId);

        /// <summary>
        /// Blocca un mezzo al termine di una corsa
        /// </summary>
        Task BloccaMezzoAsync(int idParcheggio, string idMezzo);

        /// <summary>
        /// Cambia il colore della spia di un mezzo
        /// </summary>
        Task CambiaColoreSpiaAsync(int idParcheggio, string idMezzo, ColoreSpia colore);

        // === PUBBLICAZIONE MESSAGGI GENERICI ===

        /// <summary>
        /// Pubblica un messaggio generico su un topic
        /// </summary>
        Task PublishAsync<T>(string topic, T message) where T : class;

        /// <summary>
        /// Pubblica un messaggio generico come JSON
        /// </summary>
        Task PublishAsync(string topic, string jsonMessage);

        // === EVENTI PER RICEZIONE MESSAGGI (Gateway IoT → Backend) ===

        /// <summary>
        /// Evento scatenato quando arriva un aggiornamento di status di un mezzo
        /// </summary>
        event EventHandler<MezzoStatusReceivedEventArgs>? MezzoStatusReceived;

        /// <summary>
        /// Evento scatenato quando arriva una risposta a un comando
        /// </summary>
        event EventHandler<RispostaComandoReceivedEventArgs>? RispostaComandoReceived;
    }


}
