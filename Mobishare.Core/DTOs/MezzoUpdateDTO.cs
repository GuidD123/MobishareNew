using Mobishare.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class MezzoUpdateDTO
    {
        [EnumDataType(typeof(StatoMezzo), ErrorMessage = "Stato mezzo non valido")]
        public StatoMezzo Stato { get; set; }

        [Range(0, 100, ErrorMessage = "Il livello batteria deve essere tra 0 e 100")]
        public int LivelloBatteria { get; set; }

        public int? IdParcheggioCorrente { get; set; }
    }
}
