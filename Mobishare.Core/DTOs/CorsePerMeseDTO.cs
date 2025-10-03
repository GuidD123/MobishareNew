using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class CorsaPerMeseDTO
    {
        public int Anno { get; set; }
        public int Mese { get; set; }
        public int Corse { get; set; }
        public decimal Ricavi { get; set; }
        public double DurataMediaMin { get; set; }
    }
}
