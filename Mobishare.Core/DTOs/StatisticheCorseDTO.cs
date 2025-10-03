using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mobishare.Core.DTOs;

namespace Mobishare.Core.DTOs
{
    public class StatisticheCorseDTO
    {
        public int TotaleCorse { get; set; }
        public decimal CostoTotale { get; set; }
        public double DurataMediaMinuti { get; set; }

        public Dictionary<string, int> CorsePerTipoMezzo { get; set; } = new();
        public List<CorsaPerMeseDTO> PerMese { get; set; } = new();
        public List<TopUtenteDTO> TopUtenti { get; set; } = new();
        public MezzoPiuUsatoDTO? MezzoPiuUsato { get; set; }
    }
}
