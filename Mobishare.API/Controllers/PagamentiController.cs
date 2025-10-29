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
        [Authorize(Roles = "Gestore,Utente")]
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

        
        //crea una nuova transazione il gestore -> rimborsi ecc 
        // POST: api/pagamenti
        /*[Authorize(Roles = "Gestore")]
        [HttpPost]
        public async Task<ActionResult<SuccessResponse>> PostTransazione([FromBody] TransazioneCreateDTO dto)
        {
            if (dto.Importo == 0)
                throw new ValoreNonValidoException(nameof(dto.Importo), "non può essere zero");

            var utente = await _context.Utenti.FindAsync(dto.IdUtente)
                ?? throw new ElementoNonTrovatoException("Utente", dto.IdUtente);

            // Crea transazione
            var transazione = new Transazione
            {
                IdUtente = dto.IdUtente,
                Importo = dto.Importo,
                Stato = StatoPagamento.Completato, 
                DataTransazione = DateTime.UtcNow,
                Tipo = dto.Tipo,
                IdCorsa = dto.IdCorsa,
                IdRicarica = dto.IdRicarica
            };

            _context.Transazioni.Add(transazione);

            //Aggiorna il credito dell'utente
            utente.Credito += dto.Importo; // Positivo = aggiunge, Negativo = sottrae

            //Gestisci sospensione/riattivazione
            if (utente.Sospeso && utente.Credito >= 0)
            {
                utente.Sospeso = false;
                _logger.LogInformation("Utente {UserId} riattivato tramite transazione manuale", utente.Id);
            }
            else if (!utente.Sospeso && utente.Credito < 0)
            {
                utente.Sospeso = true;
                _logger.LogWarning("Utente {UserId} sospeso tramite transazione manuale", utente.Id);
            }


            await _context.SaveChangesAsync();


            //NOTIFICA SIGNALR
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
                        TransazioneId = transazione.Id,
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


            var responseDto = new TransazioneResponseDTO
            {
                Id = transazione.Id,
                IdUtente = transazione.IdUtente,
                IdCorsa = transazione.IdCorsa,
                IdRicarica = transazione.IdRicarica,
                Importo = transazione.Importo,
                Stato = transazione.Stato.ToString(),
                DataTransazione = transazione.DataTransazione,
                Tipo = transazione.Tipo,
                NomeUtente = utente.Nome,
                EmailUtente = utente.Email
            };

            return CreatedAtAction(nameof(GetTransazioniByUtente), new { idUtente = transazione.IdUtente },
                new SuccessResponse
                {
                    Messaggio = "Transazione registrata con successo",
                    Dati = responseDto
                });
        }*/


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

                    // Notifica ai gestori
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("RiceviNotificaAdmin",
                            "Aggiornamento pagamento",
                            $"Transazione {transazione.Id} ({transazione.Importo:F2}€) aggiornata a stato: {nuovoStato}");
                

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
    }
}
