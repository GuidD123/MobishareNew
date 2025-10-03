using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Models;

namespace Mobishare.API.DTO
{
    /// <summary>
    /// DTO per avviare simulazione automatica
    /// </summary>
    public class AvviaSimulazioneAutomaticaRequest
    {
        /// <summary>
        /// Intervallo tra simulazioni in secondi
        /// </summary>
        public int IntervalloSecondi { get; set; } = 30;
    }
}
