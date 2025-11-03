using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.Enums
{
    public enum MotivoNonPrelevabile
    {
        Nessuno = 0,           // Mezzo disponibile/in uso/manutenzione
        BatteriaScarica = 1,   // Batteria < 20%
        GuastoSegnalato = 2    // Segnalato da utente
    }
}
