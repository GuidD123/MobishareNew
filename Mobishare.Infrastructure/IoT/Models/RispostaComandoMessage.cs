using Mobishare.Core.Enums;

namespace Mobishare.Infrastructure.IoT.Models
{
    /// <summary>
    /// Risposta del Gateway IoT a un comando ricevuto
    /// Topic MQTT: Parking/{id_parking}/RisposteComandi/{id_mezzo}
    /// </summary>
    public class RispostaComandoMessage
    {
        /// <summary>
        /// ID del mezzo che ha eseguito il comando
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// Comando che è stato eseguito
        /// </summary>
        public TipoComandoIoT ComandoOriginale { get; set; }

        /// <summary>
        /// Se il comando è stato eseguito con successo
        /// </summary>
        public bool Successo { get; set; }

        /// <summary>
        /// Descrizione dell'errore in caso di fallimento
        /// </summary>
        public string? ErroreDescrizione { get; set; }

        /// <summary>
        /// Timestamp della risposta
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Dati aggiuntivi restituiti dal comando
        /// </summary>
        public Dictionary<string, object>? DatiAggiuntivi { get; set; }
    }
}
