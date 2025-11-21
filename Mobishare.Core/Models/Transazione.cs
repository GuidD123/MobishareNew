using Mobishare.Core.Enums;

namespace Mobishare.Core.Models
{
    public class Transazione
    {
        public int Id { get; set; }

        // Foreign key verso Utente
        public int IdUtente { get; set; }
        public Utente? Utente { get; set; }
        // FK opzionale verso Corsa
        public int? IdCorsa { get; set; }    
        public Corsa? Corsa { get; set; }
        public int? IdRicarica { get; set; }  // Per ricariche
        public decimal Importo { get; set; }   // + ricarica, - corsa
        public StatoPagamento Stato { get; set; }   //StatoTransazione: completato, in sospeso, fallito..
        public DateTime DataTransazione { get; set; }
        public string Tipo { get; set; } = null!;
    }
}
