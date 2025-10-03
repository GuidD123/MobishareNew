using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using System.Security.Claims;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PagamentiController(MobishareDbContext context) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;

        // GET: api/pagamenti/utente/{idUtente}
        [Authorize(Roles = "gestore,utente")]
        [HttpGet("utente/{idUtente}")]
        public async Task<ActionResult<SuccessResponse>> GetTransazioniByUtente(int idUtente)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (currentUserRole != "gestore" && currentUserId != idUtente)
                throw new OperazioneNonConsentitaException("Non puoi accedere ai pagamenti di altri utenti");

            var utenteEsistente = await _context.Utenti.AnyAsync(u => u.Id == idUtente);
            if (!utenteEsistente)
                throw new ElementoNonTrovatoException("Utente", idUtente);

            var transazioni = await _context.Transazioni
                .Where(t => t.IdUtente == idUtente)
                .OrderByDescending(t => t.DataTransazione)
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista transazioni utente",
                Dati = transazioni
            });
        }

        // GET: api/pagamenti/miei
        [Authorize(Roles = "utente")]
        [HttpGet("miei")]
        public async Task<ActionResult<SuccessResponse>> GetMieTransazioni()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var pagamenti = await _context.Transazioni
                .Where(t => t.IdUtente == currentUserId)
                .OrderByDescending(t => t.DataTransazione)
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista dei miei pagamenti",
                Dati = pagamenti
            });
        }
    }

}
