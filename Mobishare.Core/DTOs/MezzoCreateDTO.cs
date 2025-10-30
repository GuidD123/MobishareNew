using Mobishare.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class MezzoCreateDTO
    {
        [Required(ErrorMessage = "La matricola è obbligatoria")]
        [StringLength(64, MinimumLength = 3, ErrorMessage = "La matricola deve essere tra 3 e 64 caratteri")]
        public string Matricola { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il tipo mezzo è obbligatorio")]
        [EnumDataType(typeof(TipoMezzo), ErrorMessage = "Tipo mezzo non valido")]
        public TipoMezzo Tipo { get; set; }

        [Required(ErrorMessage = "Lo stato mezzo è obbligatorio")]
        [EnumDataType(typeof(StatoMezzo), ErrorMessage = "Stato mezzo non valido")]
        public StatoMezzo Stato { get; set; }

        [Range(0, 100, ErrorMessage = "Il livello batteria deve essere tra 0 e 100")]
        public int LivelloBatteria { get; set; }

        [Required(ErrorMessage = "Il parcheggio corrente è obbligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "ID parcheggio non valido")]
        public int IdParcheggioCorrente { get; set; }
    }
}
