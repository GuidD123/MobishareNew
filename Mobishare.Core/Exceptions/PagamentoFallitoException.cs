using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class PagamentoFallitoException : Exception
    {
        public string Motivo { get; }

        public PagamentoFallitoException(string motivo = "Pagamento fallito")
            : base(motivo)
        {
            Motivo = motivo;
        }
    }
}
