using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class ParcheggioCreateDTO
    {
        [Required(ErrorMessage = "Il nome del parcheggio è obbligatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Il nome deve essere tra 3 e 100 caratteri")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "La zona è obbligatoria")]
        [StringLength(100, ErrorMessage = "La zona non può superare 100 caratteri")]
        public string Zona { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "L'indirizzo non può superare 255 caratteri")]
        public string? Indirizzo { get; set; }

        [Required(ErrorMessage = "La capienza è obbligatoria")]
        [Range(1, 1000, ErrorMessage = "La capienza deve essere tra 1 e 1000 posti")]
        public int Capienza { get; set; }

        public bool Attivo { get; set; } = true;
    }
}
