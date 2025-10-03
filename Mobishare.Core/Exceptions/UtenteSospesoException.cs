using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class UtenteSospesoException : Exception
    {
        public string Email { get; }

        public UtenteSospesoException(string email)
            : base($"L'utente con email {email} è sospeso e non può avviare corse")
        {
            Email = email;
        }
    }
}

