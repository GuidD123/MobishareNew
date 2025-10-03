using Mobishare.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.IoT.Gateway.Models
{
    /// <summary>
    /// Rappresenta un mezzo emulato nel Gateway
    /// </summary>
    internal class MezzoEmulato
    {
        public string IdMezzo { get; set; } = string.Empty;
        public string Matricola { get; set; } = string.Empty;
        public TipoMezzo Tipo { get; set; }
        public StatoMezzo Stato { get; set; }
        public int LivelloBatteria { get; set; }
        public ColoreSpia ColoreSpia { get; set; }
        public DateTime UltimoAggiornamento { get; set; }
    }
}
