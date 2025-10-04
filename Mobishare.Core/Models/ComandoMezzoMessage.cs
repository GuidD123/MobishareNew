using Mobishare.Core.Enums;

namespace Mobishare.Core.Models
{
    /// <summary>
    /// Comando inviato dal Backend al Gateway IoT per controllare un mezzo.
    /// Topic MQTT: Parking/{id_parking}/Comandi/{id_mezzo}
    /// </summary>
    public class ComandoMezzoMessage
    {
        /// <summary>
        /// ID univoco del mezzo destinatario
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// ID univoco del comando per idempotenza (obbligatorio)
        /// </summary>
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Tipo di comando da eseguire
        /// </summary>
        public TipoComandoIoT Comando { get; set; }

        /// <summary>
        /// Parametri aggiuntivi specifici del comando
        /// </summary>
        public Dictionary<string, object>? Parametri { get; set; }

        /// <summary>
        /// Timestamp di creazione del comando (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID dell'utente o sistema che ha inviato il comando
        /// </summary>
        public string MittenteId { get; set; } = string.Empty;
    }
}