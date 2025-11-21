using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class UtenteDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Ruolo { get; set; } = string.Empty;
        public decimal Credito { get; set; }
        public bool Sospeso { get; set; }
        public int PuntiBonus { get; set; }
    }
}
