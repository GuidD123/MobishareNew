namespace Mobishare.Core.Models
{
    public class Parcheggio
    {
        public int Id { get; set; }               // Id univoco del parcheggio
        public required string Nome { get; set; }           // Nome del parcheggio (es: "Park1", "CentroStorico")
        public required string Zona { get; set; }           // Zona della città dove si trova (es: "Centro", "Nord", ecc.)
        public string? Indirizzo { get; set; }  //indirizzo parcheggio per realismo 
        public int Capienza { get; set; }          // Numero massimo di mezzi che può contenere
        public bool Attivo { get; set; } = true;  //attivo oppure disattivo(chiuso) se in manutenzione
        public ICollection<Mezzo> Mezzi { get; set; } = []; //Posso recuperare direttamente tutti i mezzi associati a quel parcheggio 
        public ICollection<Corsa> CorsePrelievo { get; set; } = [];
        public ICollection<Corsa> CorseRilascio { get; set; } = [];

    }
}
