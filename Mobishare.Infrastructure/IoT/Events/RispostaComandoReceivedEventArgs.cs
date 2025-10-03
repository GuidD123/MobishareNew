using Mobishare.Infrastructure.IoT.Models;
using System;

namespace Mobishare.Infrastructure.IoT.Events
{
    /// <summary>
    /// Argomenti dell'evento per quando si riceve una risposta a un comando
    /// </summary>
    public class RispostaComandoReceivedEventArgs : System.EventArgs
    {
        public int IdParcheggio { get; set; }
        public string IdMezzo { get; set; } = string.Empty;
        public RispostaComandoMessage RispostaMessage { get; set; } = new();
        public string Topic { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
