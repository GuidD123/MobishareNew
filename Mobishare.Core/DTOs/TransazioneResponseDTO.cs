using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class TransazioneResponseDTO
    {
        public int Id { get; set; }
        public int IdUtente { get; set; }
        public int? IdCorsa { get; set; }
        public int? IdRicarica { get; set; }
        public decimal Importo { get; set; }
        public string Stato { get; set; } = string.Empty; // "Completato", "InSospeso", "Fallito"
        public DateTime DataTransazione { get; set; }
        public string Tipo { get; set; } = string.Empty; // "Ricarica", "Corsa", "Rimborso", ecc.

        // Campi aggiuntivi utili per la UI
        public string? NomeUtente { get; set; } // Per admin
        public string? EmailUtente { get; set; }
        public string? DescrizioneCorsa { get; set; } // es. "Corsa EB12 - 15min"

        public bool IsEntrata => Importo > 0; // true se ricarica/rimborso
        public bool IsUscita => Importo < 0;  // true se corsa/penale
    }
}
