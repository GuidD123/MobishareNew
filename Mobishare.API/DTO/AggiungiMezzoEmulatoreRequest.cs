using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Models;
using System.ComponentModel.DataAnnotations;

namespace Mobishare.API.DTO
{
    /// <summary>
    /// DTO per aggiungere un mezzo all'emulazione
    /// </summary>
    public class AggiungiMezzoEmulatorRequest
    {
        /// <summary>
        /// ID univoco del mezzo
        /// </summary>
        public string IdMezzo { get; set; } = string.Empty;

        /// <summary>
        /// Matricola del mezzo
        /// </summary>
        public string Matricola { get; set; } = string.Empty;

        /// <summary>
        /// Tipo di mezzo
        /// </summary>
        public TipoMezzo Tipo { get; set; }


        [Range(0, 100, ErrorMessage = "Il livello batteria deve essere tra 0 e 100")]
        public int LivelloBatteria { get; set; } = 100;
    }
}
