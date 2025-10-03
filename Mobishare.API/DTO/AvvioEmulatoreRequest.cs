using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Models;

namespace Mobishare.API.DTO
{
    // DTO per avviare l'emulatore del Gateway IoT
    public class AvvioEmulatoreRequest
    {
        /// <summary>
        /// ID del parcheggio da emulare
        /// </summary>
        public int IdParcheggio { get; set; }
    }
}
