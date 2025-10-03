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
        [StringLength(10, ErrorMessage = "La matricola non può superare 10 caratteri")]
        public string Matricola { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(TipoMezzo), ErrorMessage = "Tipo mezzo non valido")]
        public TipoMezzo Tipo { get; set; }

        [Required]
        [EnumDataType(typeof(StatoMezzo), ErrorMessage = "Stato mezzo non valido")]
        public StatoMezzo Stato { get; set; }

        [Range(0, 100, ErrorMessage = "Il livello batteria deve essere tra 0 e 100")]
        public int LivelloBatteria { get; set; }

        [Required]
        public int IdParcheggioCorrente { get; set; }
    }
}
