using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class AvviaCorsaDTO
    {

        [Required(ErrorMessage = "La matricola del mezzo è obbligatoria")]
        [StringLength(64, MinimumLength = 1, ErrorMessage = "La matricola deve essere tra 1 e 64 caratteri")]
        public string MatricolaMezzo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il parcheggio di prelievo è obbligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "ID parcheggio deve essere maggiore di 0")]
        public int IdParcheggioPrelievo { get; set; }
        //serve al backend per mappare correttamente il punto di partenza della corsa
    }
}
