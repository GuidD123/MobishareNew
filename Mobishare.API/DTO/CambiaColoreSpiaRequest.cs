using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Models;

namespace Mobishare.API.DTO
{
    /// <summary>
    /// DTO per cambiare il colore della spia di un mezzo
    /// </summary>
    public class CambiaColoreSpiaRequest
    {
        /// <summary>
        /// ID del parcheggio dove si trova il mezzo
        /// </summary>
        public int IdParcheggio { get; set; }

        /// <summary>
        /// ID univoco del mezzo
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// Nuovo colore della spia
        /// </summary>
        public ColoreSpia Colore { get; set; }
    }
}
