using Mobishare.Core.Enums;
using Mobishare.Core.Models;

namespace Mobishare.Core.Models
{
    public class Utente
    {
        public int Id { get; set; }          // Id univoco dell'utente
        public required string Nome { get; set; }       // Nome dell'utente
        public required string Cognome { get; set; }        //cognome utente 
        public required string Email { get; set; }          // Email dell'utente
        public required string Password { get; set; }       // semplice stringa per ora -  da cambiare con HASH
        public UserRole Ruolo { get; set; } = UserRole.Utente;     // "utente" o "gestore"
        public decimal Credito { get; set; } = 0;        // Credito disponibile dell'utente
        public decimal DebitoResiduo { get; set; } = 0;         // debito non saldato (se > 0, l'utente resta sospeso)
        public bool Sospeso { get; set; } = false;      // Se l'utente è sospeso per credito insufficiente
        public int PuntiBonus { get; set; } = 0;

        //Utili per Frontend admin/gestore per vedere storico corse e pagamenti di un utente 
        public ICollection<Corsa> Corse { get; set; } = [];     //mi permette di visusalizzare tutte le corse fatte da un utente (senza la collection dovrei fare una query separata su Corsa filtrando per IdUtente)
        public ICollection<Ricarica> RicaricheUtente { get; set; } = [];     //mi permette di avere lo storico delle ricariche di un utente 
    }
}
