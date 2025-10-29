using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class FeedbackResponseDTO
    {
        public int Id { get; set; }
        public int IdUtente { get; set; }
        public int IdCorsa { get; set; }
        public string Valutazione { get; set; } = string.Empty;
        public int ValutazioneNumero { get; set; }
        public string? Commento { get; set; }
        public DateTime DataFeedback { get; set; }
        public string? NomeUtente { get; set; }
        public string? MatricolaMezzo { get; set; }
    }

    public class FeedbackNegativiResponseDTO
    {
        public int TotaleFeedbackNegativi { get; set; }
        public List<FeedbackResponseDTO> Feedbacks { get; set; } = new();
    }

    public class FeedbackStatisticheDTO
    {
        public int TotaleFeedback { get; set; }
        public double MediaGenerale { get; set; }
        public FeedbackDistribuzioneDTO Distribuzione { get; set; } = new();
        public string? Messaggio { get; set; }
    }

    public class FeedbackDistribuzioneDTO
    {
        public int Pessimo { get; set; }
        public int Scarso { get; set; }
        public int Sufficiente { get; set; }
        public int Buono { get; set; }
        public int Ottimo { get; set; }
    }
}
