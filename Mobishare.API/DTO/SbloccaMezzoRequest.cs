using Mobishare.Infrastructure.IoT.Models;

namespace Mobishare.API.DTO
{
    /// <summary>
    /// DTO per sbloccare un mezzo via API
    /// </summary>
    public class SbloccaMezzoRequest
    {
        /// <summary>
        /// ID del parcheggio dove si trova il mezzo
        /// </summary>
        public int IdParcheggio { get; set; }

        /// <summary>
        /// ID univoco del mezzo da sbloccare
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// ID dell'utente che richiede lo sblocco
        /// </summary>
        public string UtenteId { get; set; } = string.Empty;
    }
}
