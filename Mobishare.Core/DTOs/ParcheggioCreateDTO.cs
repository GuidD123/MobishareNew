using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class ParcheggioCreateDTO
    {
        public string Nome { get; set; } = string.Empty;
        public string Zona { get; set; } = string.Empty;
        public string? Indirizzo { get; set; }
        public int Capienza { get; set; }
        public bool Attivo { get; set; } = true;
    }
}
