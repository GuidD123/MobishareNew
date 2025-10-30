using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class FineCorsaDTO
    {
        [Required(ErrorMessage = "Data e ora di fine corsa obbligatoria")]
        public DateTime DataOraFineCorsa { get; set; }

        [Required(ErrorMessage = "Il parcheggio di rilascio è obbligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "ID parcheggio deve essere maggiore di 0")]
        public int IdParcheggioRilascio { get; set; }

        public bool SegnalazioneProblema { get; set; } = false;
    }
}
    