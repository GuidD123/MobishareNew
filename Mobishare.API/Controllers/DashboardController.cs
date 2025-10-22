using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Models;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController(MobishareDbContext context) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;

        // GET: api/dashboard?idUtente=99
        [Authorize(Roles ="Gestore")]
        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] int idUtente)
        {
            var gestore = await _context.Utenti.FindAsync(idUtente);
            if (gestore == null || gestore.Ruolo != UserRole.Gestore)
                return Unauthorized("Solo un gestore può accedere a questa dashboard.");

            var oggi = DateTime.Today;

            var numeroCorseTotali = await _context.Corse.CountAsync();
            var corseOggi = await _context.Corse.CountAsync(c => c.DataOraInizio.Date == oggi);
            var mezziDisponibili = await _context.Mezzi.CountAsync(m => m.Stato == StatoMezzo.Disponibile);
            var mezziInUso = await _context.Mezzi.CountAsync(m => m.Stato == StatoMezzo.InUso);
            var mezziGuasti = await _context.Mezzi.CountAsync(m => m.Stato == StatoMezzo.NonPrelevabile);
            var utentiSospesi = await _context.Utenti.CountAsync(u => u.Sospeso);
            var creditoTotaleSistema = _context.Utenti.AsEnumerable().Sum(u => u.Credito);
            var corseUltimaSettimana = await _context.Corse
                .Where(c => c.DataOraInizio >= oggi.AddDays(-7))
                .CountAsync();

            return Ok(new SuccessResponse<DashboardDTO>
            {

                Dati = new DashboardDTO
                {
                    NumeroCorseTotali = numeroCorseTotali,
                    CorseOggi = corseOggi,
                    CorseUltimaSettimana = corseUltimaSettimana,
                    MezziDisponibili = mezziDisponibili,
                    MezziInUso = mezziInUso,
                    MezziGuasti = mezziGuasti,
                    UtentiSospesi = utentiSospesi,
                    CreditoTotaleSistema = creditoTotaleSistema,
                    Messaggio = corseUltimaSettimana == 0 ? "Nessuna corsa effettuata nell'ultima settimana." : null,
                }
            });
        }
    }
}
