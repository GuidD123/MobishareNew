using Mobishare.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.DTOs
{
    public class NuovaRicaricaDTO
    {
        [Required(ErrorMessage = "L'ID utente è obbligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "ID utente non valido")]
        public int IdUtente { get; set; }

        [Required(ErrorMessage = "L'importo è obbligatorio")]
        [Range(5.00, 500.00, ErrorMessage = "L'importo deve essere tra 5€ e 500€")]
        public decimal ImportoRicarica { get; set; }

        [Required(ErrorMessage = "Il tipo di ricarica è obbligatorio")]
        public TipoRicarica TipoRicarica { get; set; }

        [StringLength(19, MinimumLength = 13, ErrorMessage = "Numero carta deve essere tra 13 e 19 cifre")]
        public string? NumeroCarta { get; set; }  // Per carte (offuscato)

        [EmailAddress(ErrorMessage = "Email PayPal non valida")]
        [StringLength(100, ErrorMessage = "Email troppo lunga")]
        public string? EmailPayPal { get; set; }  // Per PayPal
    }

    public class RicaricaResponseDTO
    {
        public int Id { get; set; }
        public decimal ImportoRicarica { get; set; }
        public DateTime DataRicarica { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Stato { get; set; } = string.Empty;
    }

    public class ConfermaRicaricaDTO
    {
        [Required(ErrorMessage = "Il campo successo è obbligatorio")]
        public bool Successo { get; set; }

        public string? MotivoRifiuto { get; set; }

        [Required(ErrorMessage = "Token sicurezza obbligatorio")]
        public string TokenSicurezza { get; set; } = string.Empty;
    }

    public class SaldoResponseDTO
    {
        public decimal CreditoAttuale { get; set; }
        public bool UtenteAttivo { get; set; }
        public decimal TotaleRicariche { get; set; }
        public decimal RicaricheInSospeso { get; set; }
        public decimal TotaleSpese { get; set; }
        public DateTime? UltimaRicarica { get; set; }
    }
}