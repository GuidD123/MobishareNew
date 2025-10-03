using Mobishare.Core.Enums;

namespace Mobishare.Infrastructure.IoT.Models
{
    /// <summary>
    /// Messaggio inviato dal Gateway IoT al Backend con lo stato di un mezzo
    /// Topic MQTT: Parking/{id_parking}/Mezzi
    /// </summary>
    public class MezzoStatusMessage
    {

        public string IdMezzo { get; set; } = string.Empty;
        public string Matricola { get; set; } = string.Empty; 
        public int LivelloBatteria { get; set; }
        public StatoMezzo Stato { get; set;  }
        public TipoMezzo Tipo { get; set; }
        public DateTime Timestamp { get; set; }= DateTime.UtcNow;
        public string? Messaggio { get; set; }
    }
}
