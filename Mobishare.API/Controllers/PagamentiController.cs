using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.SignalRHubs;
using System.Security.Claims;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PagamentiController(MobishareDbContext context, IHubContext<NotificheHub> hubContext,
    ILogger<PagamentiController> logger) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly IHubContext<NotificheHub> _hubContext = hubContext;
        private readonly ILogger<PagamentiController> _logger = logger;


        //CONSULTA STORICO TRANSAZIONI
        // GET: api/pagamenti/utente/{idUtente}
        [Authorize(Roles = "Gestore")]
        [HttpGet("utente/{idUtente}")]
        public async Task<ActionResult<SuccessResponse>> GetTransazioniByUtente(int idUtente)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (currentUserRole != "Gestore" && currentUserId != idUtente)
                throw new OperazioneNonConsentitaException("Non puoi accedere ai pagamenti di altri utenti");

            var utenteEsistente = await _context.Utenti.AnyAsync(u => u.Id == idUtente);
            if (!utenteEsistente)
                throw new ElementoNonTrovatoException("Utente", idUtente);

            var transazioni = await _context.Transazioni
                .Include(t => t.Utente)
                .Where(t => t.IdUtente == idUtente)
                .OrderByDescending(t => t.DataTransazione)
                .Select(t => new TransazioneResponseDTO
                {
                   Id = t.Id,
                   IdUtente = t.IdUtente,
                   IdCorsa = t.IdCorsa,
                   IdRicarica = t.IdRicarica,
                   Importo = t.Importo,
                   Stato = t.Stato.ToString(),
                   DataTransazione = t.DataTransazione,
                   Tipo = t.Tipo,
                   NomeUtente = t.Utente != null ? t.Utente.Nome : "Utente sconosciuto",
                   EmailUtente = t.Utente != null ? t.Utente.Email : null
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista transazioni utente",
                Dati = transazioni
            });
        }

        //per consultare storico transazioni
        // GET: api/pagamenti/miei
        [Authorize(Roles = "Utente")]
        [HttpGet("miei")]
        public async Task<ActionResult<SuccessResponse>> GetMieTransazioni()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var transazioni = await _context.Transazioni
                .Include(t => t.Utente)
                .Where(t => t.IdUtente == currentUserId)
                .OrderByDescending(t => t.DataTransazione)
                .Select(t => new TransazioneResponseDTO
                {
                    Id = t.Id,
                    IdUtente = t.IdUtente,
                    IdCorsa = t.IdCorsa,
                    IdRicarica = t.IdRicarica,
                    Importo = t.Importo,
                    Stato = t.Stato.ToString(),
                    DataTransazione = t.DataTransazione,
                    Tipo = t.Tipo,
                    NomeUtente = t.Utente != null ? t.Utente.Nome : "Utente sconosciuto",
                    EmailUtente = t.Utente != null ? t.Utente.Email : null
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista delle mie transazioni",
                Dati = transazioni
            });
        }


        /*//aggiorna stato transazione -> Completato, Fallito ecc..
        //Invia due notifiche SignalR -> all'utente: PagamentoCompletato/PagamentoFallito
        //all'admin: RiceviNotifica
        //Restituisce SuccessResponse coi dettagli aggiornati 
        // PUT: api/pagamenti/{id} -> aggiorna stato pagamento (es. completato/fallito)
        [Authorize(Roles = "Gestore")]
        [HttpPut("{id}")]
        public async Task<ActionResult<SuccessResponse>> AggiornaTransazione(int id, [FromBody] StatoPagamento nuovoStato)
        {
            var transazione = await _context.Transazioni
                .Include(t => t.Utente)
                .FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new ElementoNonTrovatoException("Transazione", id);

            var statoVecchio = transazione.Stato;
            // Aggiorna solo se lo stato è effettivamente cambiato
            if (statoVecchio == nuovoStato)
            {
                return Ok(new SuccessResponse
                {
                    Messaggio = "Transazione già nello stato richiesto",
                    Dati = new
                    {
                        transazione.Id,
                        transazione.IdUtente,
                        transazione.Importo,
                        Stato = transazione.Stato.ToString()
                    }
                });
            }
            transazione.Stato = nuovoStato;
            await _context.SaveChangesAsync();

            try
            {
                    string evento = nuovoStato switch
                    {
                        StatoPagamento.Completato => "PagamentoCompletato",
                        StatoPagamento.Fallito => "PagamentoFallito",
                        _ => "PagamentoAggiornato"
                    };

                    await _hubContext.Clients.Group($"utenti:{transazione.IdUtente}")
                        .SendAsync(evento, new
                        {
                            TransazioneId = transazione.Id,
                            Importo = transazione.Importo,
                            Stato = nuovoStato.ToString(),
                            Data = transazione.DataTransazione
                        });
                

                _logger.LogInformation("Transazione {Id} aggiornata da {Old} a {New}", id, statoVecchio, nuovoStato);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore invio notifica SignalR per transazione {Id}", id);
            }

            return Ok(new SuccessResponse
            {
                Messaggio = $"Transazione aggiornata con successo ({nuovoStato})",
                Dati = new
                {
                    transazione.Id,
                    transazione.IdUtente,
                    transazione.Importo,
                    Stato = transazione.Stato.ToString()
                }
            });
        }*/
    }
}
