using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class BatteriaDTO
    {
        [Required]
        public int IdMezzo { get; set; }

        [Range(0, 100, ErrorMessage = "Il livello della batteria deve essere compreso tra 0 e 100.")]
        public int LivelloBatteria { get; set; }
    }
}
