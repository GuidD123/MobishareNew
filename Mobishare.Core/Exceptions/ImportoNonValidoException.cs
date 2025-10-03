using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{
    public class ImportoNonValidoException : Exception
    {
        public decimal Importo { get; }

        public ImportoNonValidoException(decimal importo, string motivo)
            : base($"Importo {importo} non valido: {motivo}")
        {
            Importo = importo;
        }
    }
}
