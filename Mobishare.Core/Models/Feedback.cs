using Mobishare.Core.Enums;

namespace Mobishare.Core.Models
{
    public class Feedback
    {
        public int Id { get; set; } // Id univoco del feedback
        public int IdUtente { get; set; } // FK verso Utente
        public int IdCorsa { get; set; } // FK verso Corsa
        public ValutazioneFeedback Valutazione { get; set; } // 1 a 5
        public string? Commento { get; set; }
        public DateTime DataFeedback { get; set; } = DateTime.Now;
        public Utente? Utente { get; set; }
        public Corsa? Corsa { get; set; }
    }
}
