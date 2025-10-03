using Mobishare.Core.Enums;

namespace Mobishare.Infrastructure.IoT.Models
{
    /// <summary>
    /// Comando inviato dal Backend al Gateway IoT per controllare un mezzo
    /// Topic MQTT: Parking/{id_parking}/StatoMezzi/{id_mezzo}
    /// </summary>
    public class ComandoMezzoMessage
    {
        public string IdMezzo { get; set; } = string.Empty; 
        public TipoComandoIoT Comando { get; set; }
        public Dictionary<string, object>? Parametri { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID dell'utente/sistema che ha inviato il comando
        /// </summary>
        public string MittenteId { get; set; } = string.Empty; 
    }
}
