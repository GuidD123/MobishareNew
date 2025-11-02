using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Core.DTOs;

public class DashboardDTO
{
    public int NumeroCorseTotali { get; set; }
    public int CorseOggi { get; set; }
    public int CorseUltimaSettimana { get; set; }
    public int MezziDisponibili { get; set; }
    public int MezziInUso { get; set; }
    public int MezziGuasti { get; set; }
    public int UtentiSospesi { get; set; }
    public int UtentiTotali { get; set; }
    public decimal CreditoTotaleSistema { get; set; }
    public string? Messaggio { get; set; }
}