using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class CorsaResponseDTO
    {
        public int Id { get; set; }
        public int IdUtente { get; set; }
        public string MatricolaMezzo { get; set; } = string.Empty;
        public int? IdParcheggioPrelievo { get; set; }
        public int? IdParcheggioRilascio { get; set; }
        public DateTime DataOraInizio { get; set; }
        public DateTime? DataOraFine { get; set; }
        public decimal? CostoFinale { get; set; }
    }
}
