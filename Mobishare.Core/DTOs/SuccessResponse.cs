using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    //non generico -> uso quando voglio restituire un object qualunque 
    public class SuccessResponse
    {
        public string? Messaggio { get; set; } = string.Empty;
        public object? Dati { get; set; }
    }

    //generico -> uso quando so già il tipo di dati 
    public class SuccessResponse<T>
    {
        public string Messaggio { get; set; } = string.Empty;
        public T? Dati { get; set; }
    }
}
