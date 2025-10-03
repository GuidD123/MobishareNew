using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Models;

namespace Mobishare.API.DTO
{
    /// <summary>
    /// DTO per inviare un comando generico a un mezzo
    /// </summary>
    public class InviaComandoRequest
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
        /// Tipo di comando da inviare
        /// </summary>
        public TipoComandoIoT Comando { get; set; }

        /// <summary>
        /// Parametri aggiuntivi per il comando (opzionale)
        /// </summary>
        public Dictionary<string, object>? Parametri { get; set; }

        /// <summary>
        /// ID dell'utente che invia il comando
        /// </summary>
        public string UtenteId { get; set; } = string.Empty;
    }
}
