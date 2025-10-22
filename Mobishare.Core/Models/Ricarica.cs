using Mobishare.Core.Enums;

namespace Mobishare.Core.Models
{
    public class Ricarica
    {
        public int Id { get; set; }
        public int IdUtente { get; set; }    //FK -> Chi ha fatto la ricarica
        public decimal ImportoRicarica { get; set; }   // Importo ricaricato
        public DateTime DataRicarica { get; set; } = DateTime.Now; 
        public TipoRicarica Tipo { get; set; } = TipoRicarica.CartaDiCredito; // Tipo(es.credito, ricarica, ecc.)
        public StatoPagamento Stato { get; set; } = StatoPagamento.InSospeso;
        public Utente? Utente { get; set; }
    }
}
