using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class OperazioneNonConsentitaException : Exception
    {
        public OperazioneNonConsentitaException()
            : base("Operazione non consentita.") { }

        public OperazioneNonConsentitaException(string message)
            : base(message) { }

        public OperazioneNonConsentitaException(string message, Exception innerException)
            : base(message, innerException) { }
    }

}
