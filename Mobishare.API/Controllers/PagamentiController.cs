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

        //per consultare storico transazioni
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

        //per consultare storico transazioni
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

        
        //crea una nuova transazione e notifica in tempo reale l’utente e i gestori
        // POST: api/pagamenti
        [Authorize(Roles = "Utente,Gestore")]
        [HttpPost]
        public async Task<ActionResult<SuccessResponse>> PostTransazione([FromBody] Transazione transazione)
        {
            if (transazione.Importo == 0)
                throw new ValoreNonValidoException(nameof(transazione.Importo), "non può essere zero");

            var utente = await _context.Utenti.FindAsync(transazione.IdUtente)
                ?? throw new ElementoNonTrovatoException("Utente", transazione.IdUtente);

            transazione.DataTransazione = DateTime.Now;
            _context.Transazioni.Add(transazione);
            await _context.SaveChangesAsync();

            try
            {
                // Notifica utente
                string evento = transazione.Stato switch
                {
                    StatoPagamento.Completato => "PagamentoCompletato",
                    StatoPagamento.Fallito => "PagamentoFallito",
                    _ => "PagamentoAggiornato"
                };

                await _hubContext.Clients.Group($"utenti:{transazione.IdUtente}")
                    .SendAsync(evento, new
                    {
                        Importo = transazione.Importo,
                        Stato = transazione.Stato.ToString(),
                        Data = transazione.DataTransazione
                    });

                // Notifica admin
                await _hubContext.Clients.Group("admin")
                    .SendAsync("RiceviNotificaAdmin",
                        "Nuovo pagamento registrato",
                        $"Utente {utente.Nome} (ID {utente.Id}) - {transazione.Importo:F2}€ [{transazione.Stato}]");

                _logger.LogInformation("Notifica SignalR inviata per transazione {Id}, utente {UtenteId}, stato {Stato}",
                    transazione.Id, transazione.IdUtente, transazione.Stato);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore durante invio notifica SignalR per transazione {Id}", transazione.Id);
            }

            return CreatedAtAction(nameof(GetTransazioniByUtente), new { idUtente = transazione.IdUtente },
                new SuccessResponse
                {
                    Messaggio = "Transazione registrata con successo",
                    Dati = transazione
                });
        }


        //aggiorna stato transazione -> Completato, Fallito ecc..
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
            transazione.Stato = nuovoStato;
            await _context.SaveChangesAsync();

            try
            {
                // Notifica all’utente (solo se è cambiato qualcosa)
                if (statoVecchio != nuovoStato)
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

                    // Notifica ai gestori
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("RiceviNotificaAdmin",
                            "Aggiornamento pagamento",
                            $"Transazione {transazione.Id} ({transazione.Importo:F2}€) aggiornata a stato: {nuovoStato}");
                }

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
        }

        /*//collegamento al controller CorseController per gestire il pagamento della corsa al suo termine 
        public async Task<Transazione> RegistraPagamentoCorsaAsync(int idUtente, decimal importo, int idCorsa)
        {
            var transazione = new Transazione
            {
                IdUtente = idUtente,
                Importo = importo,
                Stato = StatoPagamento.Completato,
                DataTransazione = DateTime.Now,
                IdCorsa = idCorsa
            };

            _context.Transazioni.Add(transazione);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"utenti:{idUtente}")
                .SendAsync("PagamentoCompletato", new
                {
                    Importo = importo,
                    Stato = "Completato",
                    Data = transazione.DataTransazione
                });

            return transazione;
        }*/


    }

}
