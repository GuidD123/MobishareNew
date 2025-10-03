namespace Mobishare.Core.DTOs
{
    public class ErrorResponse
    {
        public string Errore { get; set; } = string.Empty;

        // Campi opzionali null se non usati
        public decimal? Saldo { get; set; }
        public decimal? Richiesto { get; set; }

        public string? Mezzo { get; set; }
        public string? Email { get; set; }

        //campi per validazioni
        public int? Valore { get; set; }        // es. Livello batteria
        public string? Matricola { get; set; }  // es. matricola duplicata
        public int? Id { get; set; }            // es. id parcheggio non trovato
    }
}