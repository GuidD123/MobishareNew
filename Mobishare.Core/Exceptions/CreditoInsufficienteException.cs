using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class CreditoInsufficienteException : Exception
    {
        public decimal Saldo { get; }
        public decimal ImportoRichiesto { get; }
        public CreditoInsufficienteException(decimal saldo, decimal importo)
            : base($"Credito insufficiente: saldo {saldo}, richiesti {importo}")
        {
            Saldo = saldo;
            ImportoRichiesto = importo;
        }
    }
}
