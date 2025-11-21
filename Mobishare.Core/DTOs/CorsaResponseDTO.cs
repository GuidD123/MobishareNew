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
        public string TipoMezzo { get; set; } = string.Empty;
        public int? IdParcheggioPrelievo { get; set; }
        public string? NomeParcheggioPrelievo { get; set; }
        public int? IdParcheggioRilascio { get; set; }
        public string? NomeParcheggioRilascio { get; set; }
        public DateTime DataOraInizio { get; set; }
        public DateTime? DataOraFine { get; set; }
        public decimal? CostoFinale { get; set; }
        public int? PuntiGuadagnati { get; set; } //popolato quando si è alla fine di una corsa con bici muscolare 
        public int? PuntiUsati { get; set; } //popolato con calcolo (punti tolti) quando utente avvia nuova corsa (con altri mezzi) e usa i punti da scalare al costo finale
    }
}
