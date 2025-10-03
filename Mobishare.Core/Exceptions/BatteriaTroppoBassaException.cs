using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Exceptions
{

    public class BatteriaTroppoBassaException : Exception
    {
        public BatteriaTroppoBassaException()
            : base("Mezzi elettrici con batteria sotto il 20% non possono essere disponibili.") { }
    }
}
