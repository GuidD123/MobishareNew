using Mobishare.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Mobishare.Core.Models
{
    public class Mezzo
    {
        public int Id { get; set; }           // ID univoco del mezzo
        public required string Matricola { get; set; }    // Matricola del mezzo (ID univoco)
        public TipoMezzo Tipo { get; set; } // Tipo di mezzo (bici muscolare, bici elettrica, monopattino elettrico)
        public StatoMezzo Stato { get; set; } // Stato del mezzo (disponibile, in uso, non prelevabile)
        public int LivelloBatteria { get; set; } // Percentuale carica batteria (solo mezzi elettrici) -> 0-100
        public int IdParcheggioCorrente { get; set; } // FK verso Parcheggio corrente (dove si trova il mezzo)
        public Parcheggio? ParcheggioCorrente { get; set; } // Relazione con il parcheggio corrente
        public ICollection<Corsa> Corse { get; set; } = [];

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
