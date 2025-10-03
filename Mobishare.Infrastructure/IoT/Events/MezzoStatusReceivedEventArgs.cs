using Mobishare.Infrastructure.IoT.Models;

namespace Mobishare.Infrastructure.IoT.Events
{
    /// <summary>
    /// Argomenti dell'evento per quando si riceve un messaggio di status di un mezzo.
    /// </summary>
    public class MezzoStatusReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// ID del parcheggio da cui proviene il messaggio
        /// </summary>
        public int IdParcheggio { get; set; }

        /// <summary>
        /// Dati di stato del mezzo ricevuti
        /// </summary>
        public MezzoStatusMessage StatusMessage { get; set; } = new();

        /// <summary>
        /// Topic MQTT completo da cui è arrivato il messaggio
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp di ricezione del messaggio da parte del backend (UTC)
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}