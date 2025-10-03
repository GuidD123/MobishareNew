using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class CorsaNonTrovataException : Exception
    {
        public int Id { get; }

        public CorsaNonTrovataException(int id)
            : base($"Corsa con id {id} non trovata.")
        {
            Id = id;
        }
    }
}
