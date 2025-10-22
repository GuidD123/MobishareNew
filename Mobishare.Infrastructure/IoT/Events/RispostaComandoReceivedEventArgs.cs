using Mobishare.Core.Models;


namespace Mobishare.Infrastructure.IoT.Events
{
    /// <summary>
    /// Argomenti dell'evento per quando si riceve una risposta a un comando.
    /// </summary>
    public class RispostaComandoReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// ID del parcheggio da cui proviene la risposta
        /// </summary>
        public int IdParcheggio { get; set; }

        /// <summary>
        /// ID del mezzo che ha risposto
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// Dettagli della risposta al comando
        /// </summary>
        public RispostaComandoMessage RispostaMessage { get; set; } = new();

        /// <summary>
        /// Topic MQTT completo da cui è arrivato il messaggio
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp di ricezione della risposta da parte del backend (UTC)
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
    }
}