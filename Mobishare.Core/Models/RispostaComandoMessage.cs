using Mobishare.Core.Enums;

namespace Mobishare.Core.Models
{
    /// <summary>
    /// Risposta del Gateway IoT a un comando ricevuto.
    /// Topic MQTT: Parking/{id_parking}/RisposteComandi/{id_mezzo}
    /// </summary>
    public class RispostaComandoMessage
    {
        /// <summary>
        /// ID del mezzo che ha eseguito il comando
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// ID del comando originale (per correlazione)
        /// </summary>
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Tipo di comando che è stato eseguito
        /// </summary>
        public TipoComandoIoT ComandoOriginale { get; set; }

        /// <summary>
        /// Indica se il comando è stato eseguito con successo
        /// </summary>
        public bool Successo { get; set; }

        /// <summary>
        /// Descrizione dell'errore in caso di fallimento
        /// </summary>
        public string? ErroreDescrizione { get; set; }

        /// <summary>
        /// Timestamp della risposta (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Dati aggiuntivi restituiti dal comando
        /// </summary>
        public Dictionary<string, object>? DatiAggiuntivi { get; set; }
    }
}