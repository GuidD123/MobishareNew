using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.Models;
using Mobishare.Core.Enums;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController(MobishareDbContext context) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;

        // GET: api/dashboard?idUtente=99
        [Authorize(Roles ="gestore")]
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
            var creditoTotaleSistema = await _context.Utenti.SumAsync(u => u.Credito);
            var corseUltimaSettimana = await _context.Corse
                .Where(c => c.DataOraInizio >= oggi.AddDays(-7))
                .CountAsync();

            return Ok(new
            {
                numeroCorseTotali,
                corseOggi,
                corseUltimaSettimana,
                mezziDisponibili,
                mezziInUso,
                mezziGuasti,
                utentiSospesi,
                creditoTotaleSistema,
                messaggio = corseUltimaSettimana == 0 ? "Nessuna corsa effettuata nell'ultima settimana." : null
            });
        }
    }
}
