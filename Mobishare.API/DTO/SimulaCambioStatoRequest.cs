using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Models;

namespace Mobishare.API.DTO
{
    /// <summary>
    /// DTO per simulare un cambio di stato
    /// </summary>
    public class SimulaCambioStatoRequest
    {
        /// <summary>
        /// ID del mezzo
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// Nuovo stato del mezzo
        /// </summary>
        public StatoMezzo NuovoStato { get; set; }
    }
}
