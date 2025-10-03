using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class ElementoNonTrovatoException : Exception
    {
        public ElementoNonTrovatoException(string tipo, object id)
            : base($"{tipo} con identificativo '{id}' non trovato") { }
    }
}
