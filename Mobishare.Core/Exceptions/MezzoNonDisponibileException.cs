using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class MezzoNonDisponibileException : Exception
    {
        public string IdMezzo { get; }

        public MezzoNonDisponibileException(string idMezzo)
            : base($"Il mezzo {idMezzo} non è disponibile")
        {
            IdMezzo = idMezzo;
        }
    }
}
