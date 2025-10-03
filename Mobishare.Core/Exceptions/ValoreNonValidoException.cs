using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class ValoreNonValidoException : Exception
    {
        public ValoreNonValidoException(string campo, string motivo)
            : base($"Valore non valido per il campo '{campo}': {motivo}") { }
    }
}
