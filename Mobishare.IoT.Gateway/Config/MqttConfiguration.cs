//gestisce parametri base (host, porta, TLS, credenziali, keepalive)

namespace Mobishare.IoT.Gateway.Config
{
    /// <summary>
    /// Configurazione per la connessione MQTT
    /// </summary>
    public class MqttConfiguration
    {
        /// <summary>
        /// Indirizzo del broker MQTT (es. localhost, mqtt.eclipse.org)
        /// </summary>
        public string BrokerHost { get; set; } = "localhost";

        /// <summary>
        /// Porta del broker MQTT (default 1883)
        /// </summary>
        public int BrokerPort { get; set; } = 1883;

        /// <summary>
        /// Username per l'autenticazione (opzionale)
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Password per l'autenticazione (opzionale)
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Usa connessione TLS/SSL
        /// </summary>
        public bool UseTls { get; set; } = false;

        /// <summary>
        /// ID univoco del client MQTT
        /// </summary>
        public string ClientId { get; set; } = "MobishareBackend";

        /// <summary>
        /// Intervallo keep-alive in secondi
        /// </summary>
        public int KeepAliveSeconds { get; set; } = 60;

        /// <summary>
        /// Ritardo prima di tentare la riconnessione (ms)
        /// </summary>
        public int ReconnectDelay { get; set; } = 5000;
    }
}