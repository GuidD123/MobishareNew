using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class FineCorsaDTO
    {
        [Required]
        public DateTime DataOraFineCorsa { get; set; }

        [Required]
        public int IdParcheggioRilascio { get; set; }

        public bool SegnalazioneProblema { get; set; } = false;
    }
}
    