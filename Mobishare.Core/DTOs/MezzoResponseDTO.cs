using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//per risposte API
namespace Mobishare.Core.DTOs
{
    public class MezzoResponseDTO
    {
        public int Id { get; set; }
        public string Matricola { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Stato { get; set; } = string.Empty;
        public int? LivelloBatteria { get; set; }
        public int? IdParcheggioCorrente { get; set; }
        public string? NomeParcheggio { get; set; }
        public string? ZonaParcheggio { get; set; }
        public string? IndirizzoParcheggio { get; set; }
        public string? MotivoNonPrelevabile { get; set; } 
    }
}
