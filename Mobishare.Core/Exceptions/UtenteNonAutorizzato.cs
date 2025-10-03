using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class UtenteNonAutorizzatoException : Exception
    {
        public UtenteNonAutorizzatoException(string operazione)
            : base($"Utente non autorizzato a eseguire: {operazione}") { }
    }
}
