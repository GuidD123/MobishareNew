using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class ElementoDuplicatoException : Exception
    {
        public ElementoDuplicatoException(string tipo, string chiave)
            : base($"{tipo} con chiave '{chiave}' esiste già") { }
    }
}
