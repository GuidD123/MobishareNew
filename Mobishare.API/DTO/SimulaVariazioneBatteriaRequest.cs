using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Models;


namespace Mobishare.API.DTO
{
    /// <summary>
    /// DTO per simulare variazione batteria
    /// </summary>
    public class SimulaVariazioneBatteriaRequest
    {
        /// <summary>
        /// ID del mezzo
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// Nuovo livello batteria (0-100)
        /// </summary>
        public int NuovoLivello { get; set; }
    }
}
