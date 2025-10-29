using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class FeedbackCreateDTO
    {
        public int IdUtente { get; set; }
        public int IdCorsa { get; set; }
        public int Valutazione { get; set; } // 1-5 (corrispondente all'enum ValutazioneFeedback)
        public string? Commento { get; set; }
    }
}
