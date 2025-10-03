using Mobishare.Infrastructure.IoT.Models;
using System;

//evento per ricezione messaggi di stato.
namespace Mobishare.Infrastructure.IoT.Events
{
    /// <summary>
    /// Argomenti dell'evento per quando si riceve un messaggio di status di un mezzo
    /// </summary>
    public class MezzoStatusReceivedEventArgs : System.EventArgs
    {
        public int IdParcheggio { get; set; }
        public MezzoStatusMessage StatusMessage { get; set; } = new();
        public string Topic { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
