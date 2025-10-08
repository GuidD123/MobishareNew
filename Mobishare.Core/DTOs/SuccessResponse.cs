using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs
{
    public class SuccessResponse
    {
        public string Messaggio { get; set; } = string.Empty;
        public object? Dati { get; set; }
    }

    public class SuccessResponse<T>
    {
        public string Messaggio { get; set; } = string.Empty;
        public T? Dati { get; set; }
    }
}
