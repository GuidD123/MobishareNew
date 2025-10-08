using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.IoT.Interfaces;
using Mobishare.Infrastructure.SignalRHubs;
using Mobishare.Infrastructure.SignalRHubs.Services;
using System.Security.Claims;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorseController(MobishareDbContext context, IMqttIoTService mqttIoTService, ILogger<CorseController> logger, IHubContext<NotificheHub> hubContext, NotificationOutboxService notifiche) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly IMqttIoTService _mqttIoTService = mqttIoTService;
        private readonly ILogger<CorseController> _logger = logger;
        private readonly IHubContext<NotificheHub> _hubContext = hubContext;
        private readonly NotificationOutboxService _notifiche = notifiche;


        // GET: api/corse? idUtente=& matricolaMezzo=..
        [Authorize(Roles = "Gestore")]
        [HttpGet]
        public async Task<ActionResult<SuccessResponse>> GetCorse([FromQuery] int? idUtente, [FromQuery] string? matricolaMezzo)
        {
            var query = _context.Corse.AsNoTracking().AsQueryable();

            if (idUtente.HasValue)
            {
                if(idUtente.Value <= 0)
                    throw new ValoreNonValidoException(nameof(idUtente), "deve essere maggiore di 0");
                query = query.Where(c => c.IdUtente == idUtente.Value);
            }

            if (!string.IsNullOrWhiteSpace(matricolaMezzo))
            {
                var mat = matricolaMezzo.Trim();
                if (mat.Length > 64)
                    throw new ValoreNonValidoException(nameof(matricolaMezzo), "lunghezza massima 64");
                query = query.Where(c => c.MatricolaMezzo == mat);
            }

            var corse = await query
                .OrderByDescending(c => c.DataOraInizio)
                .Select(c => new CorsaResponseDTO
                {
                    Id = c.Id,
                    IdUtente = c.IdUtente,
                    MatricolaMezzo = c.MatricolaMezzo,
                    IdParcheggioPrelievo = c.IdParcheggioPrelievo,
                    IdParcheggioRilascio = c.IdParcheggioRilascio,
                    DataOraInizio = c.DataOraInizio,
                    DataOraFine = c.DataOraFine,
                    CostoFinale = c.CostoFinale
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista corse",
                Dati = corse
            });
        }



        // GET: api/corse/utente/{idUtente} -> storico corse di un utente
        [Authorize]
        [HttpGet("utente/{idUtente:int}")]
        public async Task<ActionResult<SuccessResponse>> GetStoricoCorseUtente(int idUtente)
        {
            //verifica input idUtente 
            if (idUtente <= 0)
                throw new ValoreNonValidoException(nameof(idUtente), "deve essere maggiore di 0");

            //legge idUtente dal JWT (claim) -> int.Parse(..) lo converte in int, se manca il claim -> lancia una 403 OperazioneNonConsentita
            var callerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new OperazioneNonConsentitaException("Utente non autenticato"));
            var isGestore = User.IsInRole("Gestore"); //verifica ruolo da tokens
            if (!isGestore && callerId != idUtente) //consente accesso solo se il chiamante è il proprietario della corsa oppure è un Gestore (vede le corse di tutti)
                throw new OperazioneNonConsentitaException("Non autorizzato a visualizzare corse di altri utenti");

            // Verifica che l'utente esista
            var utente = await _context.Utenti.AsNoTracking().FirstOrDefaultAsync(u => u.Id == idUtente)
                ?? throw new ElementoNonTrovatoException("Utente", idUtente);

            // Recupera le corse di quell'utente
            var corse = await _context.Corse.AsNoTracking()
                .Where(c => c.IdUtente == idUtente)
                .OrderByDescending(c => c.DataOraInizio) // ordine: più recenti prima
                .Select(c => new CorsaResponseDTO
                {
                    Id = c.Id,
                    IdUtente = c.IdUtente,
                    MatricolaMezzo = c.MatricolaMezzo,
                    IdParcheggioPrelievo = c.IdParcheggioPrelievo,
                    IdParcheggioRilascio = c.IdParcheggioRilascio,
                    DataOraInizio = c.DataOraInizio,
                    DataOraFine = c.DataOraFine,
                    CostoFinale = c.CostoFinale
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = $"Storico corse per utente {utente.Nome} {utente.Cognome}",
                Dati = corse
            });
        }



        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SuccessResponse>> GetCorsaById(int id)
        {
            if (id <= 0)
                throw new ValoreNonValidoException(nameof(id), "deve essere maggiore di 0");

            var corsa = await _context.Corse.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new ElementoNonTrovatoException("Corsa", id);

            var callerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new OperazioneNonConsentitaException("Utente non autenticato"));
            var isGestore = User.IsInRole("Gestore");
            if (!isGestore && corsa.IdUtente != callerId)
                throw new OperazioneNonConsentitaException("Non autorizzato a visualizzare questa corsa");

            var dto = new CorsaResponseDTO
            {
                Id = corsa.Id,
                IdUtente = corsa.IdUtente,
                MatricolaMezzo = corsa.MatricolaMezzo,
                IdParcheggioPrelievo = corsa.IdParcheggioPrelievo,
                IdParcheggioRilascio = corsa.IdParcheggioRilascio,
                DataOraInizio = corsa.DataOraInizio,
                DataOraFine = corsa.DataOraFine,
                CostoFinale = corsa.CostoFinale
            };

            return Ok(new SuccessResponse
            {
                Messaggio = "Dettaglio corsa",
                Dati = dto
            });
        }



        // POST: api/corse -> inizio corsa 
        [Authorize(Roles = "Utente")]
        [HttpPost("inizia")]
        public async Task<ActionResult<SuccessResponse>> PostCorsa([FromBody] AvviaCorsaDTO dto)
        {   
            //Validazione input
            if (dto == null) throw new ValoreNonValidoException("body", "mancante");
            if (string.IsNullOrWhiteSpace(dto.MatricolaMezzo))
                throw new ValoreNonValidoException(nameof(dto.MatricolaMezzo), "obbligatoria");
            var matricola = dto.MatricolaMezzo.Trim();
            if (matricola.Length > 64)
                throw new ValoreNonValidoException(nameof(dto.MatricolaMezzo), "lunghezza massima 64");
            if (dto.IdParcheggioPrelievo <= 0)
                throw new ValoreNonValidoException(nameof(dto.IdParcheggioPrelievo), "deve essere > 0");


            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Recupero utente dal token JWT
                var idUtente = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new OperazioneNonConsentitaException("Utente non autenticato"));

                var utente = await _context.Utenti.FindAsync(idUtente)
                     ?? throw new ElementoNonTrovatoException("Utente", idUtente);

                // Verifica che l'utente non abbia già una corsa in corso
                bool corsaInCorso = await _context.Corse.AnyAsync(c => c.IdUtente == utente.Id && c.DataOraFine == null);
                if (corsaInCorso)
                    throw new OperazioneNonConsentitaException("Hai già una corsa in corso. Termina quella prima di avviarne un'altra.");


                // Verifiche risorse
                var parcheggio = await _context.Parcheggi.FindAsync(dto.IdParcheggioPrelievo)
                    ?? throw new ElementoNonTrovatoException("Parcheggio", dto.IdParcheggioPrelievo);

                var mezzo = await _context.Mezzi.FirstOrDefaultAsync(m => m.Matricola == dto.MatricolaMezzo) 
                    ?? throw new ElementoNonTrovatoException("Mezzo", dto.MatricolaMezzo);


                if (utente.Credito < Tariffe.COSTO_BASE)
                    throw new CreditoInsufficienteException(utente.Credito, Tariffe.COSTO_BASE);

                if (mezzo.Stato != StatoMezzo.Disponibile)
                    throw new MezzoNonDisponibileException(mezzo.Matricola);

                // Reload per sicurezza concorrenza
                await _context.Entry(mezzo).ReloadAsync();
                if (mezzo.Stato != StatoMezzo.Disponibile)
                    throw new MezzoNonDisponibileException(mezzo.Matricola);

                // Tutto ok → avvio corsa
                mezzo.Stato = StatoMezzo.InUso;

                var corsa = new Corsa
                {
                    IdUtente = utente.Id,  // ⚡ NON più dto.IdUtente
                    MatricolaMezzo = dto.MatricolaMezzo,
                    IdParcheggioPrelievo = dto.IdParcheggioPrelievo,
                    DataOraInizio = DateTime.UtcNow
                };

                _context.Corse.Add(corsa);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    // Avvia il mezzo tramite MQTT dopo commit 
                    await _mqttIoTService.SbloccaMezzoAsync(dto.IdParcheggioPrelievo, dto.MatricolaMezzo, utente.Id.ToString());
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e,
                        "MQTT SbloccaMezzo fallito. CorsaId={CorsaId} Matricola={Matricola} ParcheggioId={ParcheggioId}",
                        corsa.Id, matricola, dto.IdParcheggioPrelievo);
                }



                var response = new CorsaResponseDTO
                {
                    Id = corsa.Id,
                    IdUtente = corsa.IdUtente,
                    MatricolaMezzo = corsa.MatricolaMezzo,
                    IdParcheggioPrelievo = corsa.IdParcheggioPrelievo,
                    DataOraInizio = corsa.DataOraInizio
                };

                return CreatedAtAction(nameof(GetCorsaById), new { id = corsa.Id }, 
                    new SuccessResponse
                {
                    Messaggio = "Corsa avviata correttamente",
                    Dati = response
                });

            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                throw new OperazioneNonConsentitaException("Il mezzo è stato prenotato da un altro utente");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        // PUT: api/corse/{id} -> fine corsa + logica credito consistente
        [Authorize(Roles = "Utente,Gestore")]
        [HttpPut("{id}")]
        public async Task<ActionResult<SuccessResponse>> PutCorsa(int id, [FromBody] FineCorsaDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var corsaEsistente = await _context.Corse.FindAsync(id) ?? throw new ElementoNonTrovatoException("Corsa", id);
                if (corsaEsistente.DataOraFine.HasValue)
                    throw new OperazioneNonConsentitaException("La corsa è già terminata");

                corsaEsistente.DataOraFine = dto.DataOraFineCorsa;
                corsaEsistente.IdParcheggioRilascio = dto.IdParcheggioRilascio;

                var utente = await _context.Utenti.FindAsync(corsaEsistente.IdUtente);
                var mezzo = await _context.Mezzi.FirstOrDefaultAsync(m => m.Matricola == corsaEsistente.MatricolaMezzo);
               
                if (utente == null)
                    throw new ElementoNonTrovatoException("Utente", corsaEsistente.IdUtente);
                if (mezzo == null)
                    throw new ElementoNonTrovatoException("Mezzo", corsaEsistente.MatricolaMezzo);

                // Calcolo costo della corsa
                TimeSpan durata = corsaEsistente.DataOraFine.Value - corsaEsistente.DataOraInizio;
                decimal costo = Tariffe.COSTO_BASE;

                if (durata.TotalMinutes > 30)
                {
                    decimal minutiExtra = (decimal)durata.TotalMinutes - 30;
                    switch (mezzo.Tipo)
                    {
                        case TipoMezzo.MonopattinoElettrico:
                            costo += minutiExtra * Tariffe.COSTO_MINUTO_MONOPATTINOELETTRICO;
                            break;
                        case TipoMezzo.BiciElettrica:
                            costo += minutiExtra * Tariffe.COSTO_MINUTO_BICIELETTRICA;
                            break;
                        case TipoMezzo.BiciMuscolare:
                            costo += minutiExtra * Tariffe.COSTO_MINUTO_BICIMUSCOLARE;
                            break;
                    }
                }

                _logger.LogInformation("Fine corsa {CorsaId}: durata {DurataMinuti:F1} min, costo {Costo:F2}€, utente {UtenteId}, mezzo {Matricola}",
                    corsaEsistente.Id,
                    durata.TotalMinutes,
                    costo,
                    corsaEsistente.IdUtente,
                    corsaEsistente.MatricolaMezzo
                );


                corsaEsistente.CostoFinale = costo;

                utente.Credito -= costo;
                if (utente.Credito < 0)
                    utente.Sospeso = true;

                bool problemaSegnalato = dto.SegnalazioneProblema;
                bool batteriaScarica = (mezzo.Tipo == TipoMezzo.MonopattinoElettrico || mezzo.Tipo == TipoMezzo.BiciElettrica)
                                       && mezzo.LivelloBatteria < 20;

                mezzo.Stato = (problemaSegnalato || batteriaScarica)
                    ? StatoMezzo.NonPrelevabile
                    : StatoMezzo.Disponibile;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Publish solo dopo commit
                await _mqttIoTService.BloccaMezzoAsync(
                    corsaEsistente.IdParcheggioRilascio ?? 0,
                    corsaEsistente.MatricolaMezzo
                );

                try
                {
                    //Notifica l’utente con credito aggiornato
                    await _hubContext.Clients.Group($"utenti:{utente.Id}")
                        .SendAsync("CreditoAggiornato", utente.Credito);

                    //Notifica tutti gli admin con dettagli della corsa terminata
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("RiceviNotificaAdmin",
                            "Corsa terminata",
                            $"Utente {utente.Nome} (ID {utente.Id}) ha terminato una corsa da {corsaEsistente.CostoFinale:F2}€");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invio notifica SignalR fallito per Corsa {CorsaId}", corsaEsistente.Id);
                }

                _notifiche.Enqueue(utente.Id.ToString(),"CorsaTerminata",
                    new
                    {
                        CorsaId = corsaEsistente.Id,
                        Costo = corsaEsistente.CostoFinale,
                        NuovoCredito = utente.Credito,
                        StatoUtente = utente.Sospeso ? "Sospeso" : "Attivo",
                        DataOraFine = corsaEsistente.DataOraFine
                    }
                );

                try
                {
                    await _notifiche.FlushAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Outbox flush fallito dopo Corsa {CorsaId}", corsaEsistente.Id);
                }
                ;

                var response = new CorsaResponseDTO
                {
                    Id = corsaEsistente.Id,
                    IdUtente = corsaEsistente.IdUtente,
                    MatricolaMezzo = corsaEsistente.MatricolaMezzo,
                    IdParcheggioPrelievo = corsaEsistente.IdParcheggioPrelievo,
                    IdParcheggioRilascio = corsaEsistente.IdParcheggioRilascio,
                    DataOraInizio = corsaEsistente.DataOraInizio,
                    DataOraFine = corsaEsistente.DataOraFine,
                    CostoFinale = corsaEsistente.CostoFinale
                };

                return Ok(new SuccessResponse
                {
                    Messaggio = "Corsa terminata correttamente",
                    Dati = response
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                if (!_context.Corse.Any(e => e.Id == id))
                    throw new ElementoNonTrovatoException("Corsa", id);
                else
                    throw new OperazioneNonConsentitaException("La corsa è stata modificata da un altro processo");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // GET: api/corse/statistiche
        [HttpGet("statistiche")]
        public async Task<ActionResult<SuccessResponse>> GetStatisticheCorse(
            [FromQuery] int? idUtente,
            [FromQuery] string? matricolaMezzo,
            [FromQuery] DateTime? dal,
            [FromQuery] DateTime? al)
        {
            var q = _context.Corse.AsNoTracking().AsQueryable();

            if (idUtente.HasValue) q = q.Where(c => c.IdUtente == idUtente.Value);
            if (!string.IsNullOrWhiteSpace(matricolaMezzo)) q = q.Where(c => c.MatricolaMezzo == matricolaMezzo);
            if (dal.HasValue) q = q.Where(c => c.DataOraInizio >= dal.Value);
            if (al.HasValue) q = q.Where(c => c.DataOraInizio < al.Value);

            // join per ottenere anche il Tipo del mezzo
            var corseJoin = from c in q
                            join m in _context.Mezzi.AsNoTracking()
                                on c.MatricolaMezzo equals m.Matricola
                            select new
                            {
                                c.Id,
                                c.IdUtente,
                                c.MatricolaMezzo,
                                c.DataOraInizio,
                                c.DataOraFine,
                                c.CostoFinale,
                                m.Tipo
                            };

            var lista = await corseJoin.ToListAsync();

            // aggregazioni
            var totale = lista.Count;
            var costoTot = lista.Sum(x => x.CostoFinale ?? 0m);
            var durataMediaMin = lista
                .Where(x => x.DataOraFine.HasValue)
                .Select(x => (x.DataOraFine!.Value - x.DataOraInizio).TotalMinutes)
                .DefaultIfEmpty(0).Average();

            var corsePerTipo = lista
                .GroupBy(x => x.Tipo.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var mezzoPiuUsato = lista
                .GroupBy(x => x.MatricolaMezzo)
                .OrderByDescending(g => g.Count())
                .Select(g => new MezzoPiuUsatoDTO
                {
                    Matricola = g.Key,
                    Conteggio = g.Count()
                })
                .FirstOrDefault();

            var perMese = lista
                .GroupBy(x => new { x.DataOraInizio.Year, x.DataOraInizio.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new CorsaPerMeseDTO
                {
                    Anno = g.Key.Year,
                    Mese = g.Key.Month,
                    Corse = g.Count(),
                    Ricavi = g.Sum(x => x.CostoFinale ?? 0m),
                    DurataMediaMin = g.Where(x => x.DataOraFine.HasValue)
                                      .Select(x => (x.DataOraFine!.Value - x.DataOraInizio).TotalMinutes)
                                      .DefaultIfEmpty(0).Average()
                })
                .ToList();

            var topUtenti = lista
                .GroupBy(x => x.IdUtente)
                .Select(g => new TopUtenteDTO
                {
                    IdUtente = g.Key,
                    Corse = g.Count(),
                    Spesa = g.Sum(x => x.CostoFinale ?? 0m)
                })
                .OrderByDescending(x => x.Spesa).Take(5).ToList();

            // costruiamo il DTO finale
            var statistiche = new StatisticheCorseDTO
            {
                TotaleCorse = totale,
                CostoTotale = costoTot,
                DurataMediaMinuti = Math.Round(durataMediaMin, 1),
                CorsePerTipoMezzo = corsePerTipo,
                PerMese = perMese,
                TopUtenti = topUtenti,
                MezzoPiuUsato = mezzoPiuUsato
            };

            return Ok(new SuccessResponse
            {
                Messaggio = "Statistiche corse",
                Dati = statistiche
            });
        }


    }
}
