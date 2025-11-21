namespace Mobishare.Infrastructure.IoT.Events
{
    /// <summary>
    /// Risultato di un'operazione MQTT - es. API che ritorna esito publish
    /// </summary>
    public class MqttOperationResult
    {
        /// <summary>
        /// Indica se l'operazione è riuscita
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Messaggio di errore in caso di fallimento
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Eccezione catturata (se disponibile)
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Timestamp dell'operazione (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Factory method per operazione riuscita
        /// </summary>
        public static MqttOperationResult Ok() => new() { Success = true };

        /// <summary>
        /// Factory method per operazione fallita
        /// </summary>
        public static MqttOperationResult Error(string message, Exception? ex = null) =>
            new() { Success = false, ErrorMessage = message, Exception = ex };
    }
}