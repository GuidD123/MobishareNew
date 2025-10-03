using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class AvviaCorsaDTO
    {

        [Required]
        [StringLength(10, ErrorMessage = "La matricola del mezzo è obbligatoria e deve essere al massimo di 10 caratteri.")]
        public string MatricolaMezzo { get; set; } = string.Empty;

        [Required(ErrorMessage="Il parcheggio di prelievo è obbligatorio")]
        public int IdParcheggioPrelievo { get; set; }
        //serve al backend per mappare correttamente il punto di partenza della corsa
    }
}
