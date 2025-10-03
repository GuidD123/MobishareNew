using Mobishare.Core.Enums;

namespace Mobishare.Infrastructure.IoT.Models
{
    /// <summary>
    /// Messaggio inviato dal Gateway IoT al Backend con lo stato di un mezzo.
    /// Topic MQTT: Parking/{id_parking}/Mezzi/{id_mezzo}
    /// </summary>
    public class MezzoStatusMessage
    {
        /// <summary>
        /// ID univoco del mezzo
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// Matricola/targa del mezzo
        /// </summary>
        public string Matricola { get; set; } = string.Empty;

        /// <summary>
        /// Livello batteria (0-100%)
        /// </summary>
        public int LivelloBatteria { get; set; }

        /// <summary>
        /// Stato operativo corrente del mezzo
        /// </summary>
        public StatoMezzo Stato { get; set; }

        /// <summary>
        /// Tipologia del mezzo (monopattino, bici, ecc.)
        /// </summary>
        public TipoMezzo Tipo { get; set; }

        /// <summary>
        /// Timestamp del messaggio (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Messaggio opzionale (es. diagnostica, alert)
        /// </summary>
        public string? Messaggio { get; set; }
    }
}