using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mobishare.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class TransazioneCreateDTO
    {
        [Required(ErrorMessage = "L'ID utente è obbligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "ID utente non valido")]
        public int IdUtente { get; set; }

        [Required(ErrorMessage = "L'importo è obbligatorio")]
        public decimal Importo { get; set; } // Può essere positivo (ricarica/rimborso) o negativo (penale)

        [Required(ErrorMessage = "Il tipo di transazione è obbligatorio")]
        [StringLength(50, ErrorMessage = "Il tipo non può superare 50 caratteri")]
        public string Tipo { get; set; } = "Manuale"; // "Rimborso", "Penale", "Bonus", "Manuale"

        // FK opzionali - solo uno dei due dovrebbe essere valorizzato
        public int? IdCorsa { get; set; } // Opzionale, se transazione legata a una corsa
        public int? IdRicarica { get; set; }

        [StringLength(500,ErrorMessage = "Le note non possono superare 500 caratteri")]
        public string? Note { get; set; } // Note admin
    }
}
