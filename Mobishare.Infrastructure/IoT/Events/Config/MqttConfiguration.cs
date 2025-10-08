namespace Mobishare.Infrastructure.IoT.Events.Config
{
    /// <summary>
    /// Configurazione per la connessione MQTT.
    /// Gestisce parametri base: host, porta, TLS, credenziali, keepalive.
    /// </summary>
    public class MqttConfiguration
    {
        /// <summary>
        /// Indirizzo del broker MQTT (es. localhost, mqtt.example.com)
        /// </summary>
        public string BrokerHost { get; set; } = "localhost";

        /// <summary>
        /// Porta del broker MQTT (default: 1883 per TCP, 8883 per TLS)
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
        /// Abilita connessione TLS/SSL
        /// </summary>
        public bool UseTls { get; set; } = false;

        /// <summary>
        /// ID univoco del client MQTT (verrà suffissato con MachineName-Ticks)
        /// </summary>
        public string ClientId { get; set; } = "MobishareBackend";

        /// <summary>
        /// Intervallo keep-alive in secondi (default: 60s)
        /// </summary>
        public int KeepAliveSeconds { get; set; } = 60;

        /// <summary>
        /// Ritardo prima di tentare la riconnessione in millisecondi (default: 5000ms)
        /// </summary>
        public int ReconnectDelay { get; set; } = 5000;
    }
}