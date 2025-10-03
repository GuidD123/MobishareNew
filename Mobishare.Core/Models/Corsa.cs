using Mobishare.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.Models
{
    public class Corsa
    {
        public int Id { get; set; }    // Id univoco della corsa
        public int IdUtente { get; set; }     // FK verso Utente
        public Utente? Utente { get; set; }  //NavigationProperty verso Utente
        public StatoCorsa Stato { get; set; } = StatoCorsa.InCorso;
        public required string MatricolaMezzo { get; set; } = string.Empty;  // FK verso Mezzo
        public Mezzo? Mezzo { get; set; }   //NavigationProperty verso Mezzo
        public int IdParcheggioPrelievo { get; set; }
        public Parcheggio? ParcheggioPrelievo { get; set; } // FK verso Parcheggio prelievo
        public DateTime DataOraInizio { get; set; }  // Orario inizio corsa
        public int? IdParcheggioRilascio { get; set; }
        public Parcheggio? ParcheggioRilascio { get; set; } // FK verso Parcheggio rilascio (può essere null all'inizio)
        public DateTime? DataOraFine { get; set; }   // Orario fine corsa (può essere null finché non termina)
        public decimal? CostoFinale { get; set; } //addebito va sottratto al credito disponibile -> ottengo saldo utente 
        public bool SegnalazioneProblema { get; set; }

        // Aggiunta per la concorrenza
        [Timestamp]
        public byte[]? RowVersion { get; set; } = [];

    }
}

//Ci sono campi null perche quando inizio una corsa non so dove la finirò quindi IdParcheggioRilascio è null. Inoltre quando la inizio non so quando la finirò dunque ho DataOraFine è null