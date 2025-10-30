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
using Mobishare.Infrastructure.Services;
using Mobishare.Infrastructure.SignalRHubs;
using Mobishare.Infrastructure.SignalRHubs.Services;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorseController(MobishareDbContext context, IMqttIoTService mqttIoTService, ILogger<CorseController> logger, IHubContext<NotificheHub> hubContext, NotificationOutboxService notifiche, PagamentoService pagamentoService) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly IMqttIoTService _mqttIoTService = mqttIoTService;
        private readonly ILogger<CorseController> _logger = logger;
        private readonly IHubContext<NotificheHub> _hubContext = hubContext;
        private readonly NotificationOutboxService _notifiche = notifiche;
        private readonly PagamentoService _pagamentoService = pagamentoService;



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

            var corse = await (from c in query
                               join m in _context.Mezzi on c.MatricolaMezzo equals m.Matricola
                               orderby c.DataOraInizio descending
                               select new CorsaResponseDTO
                               {
                                   Id = c.Id,
                                   IdUtente = c.IdUtente,
                                   MatricolaMezzo = c.MatricolaMezzo,
                                   TipoMezzo = m.Tipo.ToString(),
                                   IdParcheggioPrelievo = c.IdParcheggioPrelievo,
                                   IdParcheggioRilascio = c.IdParcheggioRilascio,
                                   DataOraInizio = c.DataOraInizio,
                                   DataOraFine = c.DataOraFine,
                                   CostoFinale = c.CostoFinale
                               }).ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista corse",
                Dati = corse
            });
        }


        //legge id utente dal JWT -> ritorna corsa con DataOraFine == null -> accessibile solo da utente -> include TipoMezzo con join a Mezzo
        // GET: api/corse/attiva -> corsa in corso dell'utente autenticato
        [Authorize(Roles = "Utente")]
        [HttpGet("attiva")]
        public async Task<ActionResult<SuccessResponse>> GetCorsaAttiva()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new OperazioneNonConsentitaException("Utente non autenticato"));

            var corsa = await (from c in _context.Corse.AsNoTracking()
                               join m in _context.Mezzi.AsNoTracking()
                                   on c.MatricolaMezzo equals m.Matricola
                               where c.IdUtente == userId && c.DataOraFine == null
                               select new CorsaResponseDTO
                               {
                                   Id = c.Id,
                                   IdUtente = c.IdUtente,
                                   MatricolaMezzo = c.MatricolaMezzo,
                                   TipoMezzo = m.Tipo.ToString(),
                                   IdParcheggioPrelievo = c.IdParcheggioPrelievo,
                                   DataOraInizio = c.DataOraInizio,
                                   CostoFinale = c.CostoFinale
                               }).FirstOrDefaultAsync();

            if (corsa == null)
                return NotFound(new ErrorResponse { Messaggio = "Nessuna corsa attiva trovata." });

            return Ok(new SuccessResponse
            {
                Messaggio = "Corsa attiva trovata",
                Dati = corsa
            });
        }




        // GET: api/corse/utente/{idUtente} -> storico corse di un utente
        [Authorize]
        [HttpGet("utente/{idUtente:int}")]
        public async Task<ActionResult<SuccessResponse>> GetStoricoCorseUtente(int idUtente)
        {

            var query = _context.Corse.AsNoTracking().AsQueryable();

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
            var corse = await (from c in query
                               join m in _context.Mezzi on c.MatricolaMezzo equals m.Matricola
                               orderby c.DataOraInizio descending
                               select new CorsaResponseDTO
                               {
                                   Id = c.Id,
                                   IdUtente = c.IdUtente,
                                   MatricolaMezzo = c.MatricolaMezzo,
                                   TipoMezzo = m.Tipo.ToString(),  // ← QUESTO È IMPORTANTE!
                                   IdParcheggioPrelievo = c.IdParcheggioPrelievo,
                                   IdParcheggioRilascio = c.IdParcheggioRilascio,
                                   DataOraInizio = c.DataOraInizio,
                                   DataOraFine = c.DataOraFine,
                                   CostoFinale = c.CostoFinale
                               }).ToListAsync();

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

            // ✅ Aggiungi join
            var dto = await (from c in _context.Corse.AsNoTracking()
                             join m in _context.Mezzi on c.MatricolaMezzo equals m.Matricola
                             where c.Id == id
                             select new CorsaResponseDTO
                             {
                                 Id = c.Id,
                                 IdUtente = c.IdUtente,
                                 MatricolaMezzo = c.MatricolaMezzo,
                                 TipoMezzo = m.Tipo.ToString(),
                                 IdParcheggioPrelievo = c.IdParcheggioPrelievo,
                                 IdParcheggioRilascio = c.IdParcheggioRilascio,
                                 DataOraInizio = c.DataOraInizio,
                                 DataOraFine = c.DataOraFine,
                                 CostoFinale = c.CostoFinale
                             }).FirstOrDefaultAsync()
                ?? throw new ElementoNonTrovatoException("Corsa", id);

            var callerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new OperazioneNonConsentitaException("Utente non autenticato"));
            var isGestore = User.IsInRole("Gestore");
            if (!isGestore && dto.IdUtente != callerId)
                throw new OperazioneNonConsentitaException("Non autorizzato a visualizzare questa corsa");

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

            var matricola = dto.MatricolaMezzo.Trim();

            // ============================================
            // gestione race condition
            // Fino a 3 tentativi con delay esponenziale
            // ============================================
            const int MAX_RETRY = 3;
            int attempt = 0;

            while (attempt < MAX_RETRY)
            {
                attempt++;

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

                    // Verifiche Business Logic 
                    if (utente.Credito < Tariffe.COSTO_BASE)
                        throw new CreditoInsufficienteException(utente.Credito, Tariffe.COSTO_BASE);

                    if (mezzo.Stato != StatoMezzo.Disponibile)
                        throw new MezzoNonDisponibileException(mezzo.Matricola);

                    // Tutto ok → avvio corsa
                    mezzo.Stato = StatoMezzo.InUso;

                    var corsa = new Corsa
                    {
                        IdUtente = utente.Id,  
                        MatricolaMezzo = dto.MatricolaMezzo,
                        IdParcheggioPrelievo = dto.IdParcheggioPrelievo,
                        DataOraInizio = DateTime.Now
                    };

                    _context.Corse.Add(corsa);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    bool mqttSuccesso = false;
                    string? mqttErrore = null;

                    try
                    {
                        _logger.LogInformation("Tentativo invio comando MQTT sblocco mezzo {Matricola}...",matricola);

                        var mqttTask = _mqttIoTService.SbloccaMezzoAsync(
                            dto.IdParcheggioPrelievo,
                            matricola,
                            utente.Id.ToString());

                        var completedTask = await Task.WhenAny(
                            mqttTask,
                            Task.Delay(TimeSpan.FromSeconds(5))
                        );

                        if (completedTask == mqttTask)
                        {
                            await mqttTask; // Rilancia eccezioni se presenti
                            mqttSuccesso = true;
                            _logger.LogInformation("Comando MQTT inviato con successo");
                        }
                        else
                        {
                            mqttErrore = "Timeout: il sistema IoT non risponde";
                            _logger.LogWarning("Timeout comando MQTT per mezzo {Matricola}", matricola);
                        }
                    }
                    catch (Exception mqttEx)
                    {
                        mqttErrore = $"Errore comunicazione IoT: {mqttEx.Message}";
                        _logger.LogError(mqttEx,"Errore MQTT durante sblocco mezzo {Matricola}",matricola);
                    }

                    if (mqttSuccesso)
                    {
                        // Tutto OK: committa transazione
                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "Corsa avviata con successo. CorsaId={CorsaId} Mezzo={Matricola}",
                            corsa.Id, matricola);

                        var response = new CorsaResponseDTO
                        {
                            Id = corsa.Id,
                            IdUtente = corsa.IdUtente,
                            MatricolaMezzo = corsa.MatricolaMezzo,
                            TipoMezzo = mezzo.Tipo.ToString(),
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
                    else
                    {
                        //MQTT fallito: ROLLBACK completo
                        await transaction.RollbackAsync();

                        _logger.LogWarning(
                            "Rollback corsa per fallimento MQTT. Mezzo={Matricola} Errore={Errore}",
                            matricola, mqttErrore);

                        // Solleva eccezione che verrà catturata dal middleware
                        throw new OperazioneNonConsentitaException(
                            $"Impossibile sbloccare il mezzo: {mqttErrore}. " +
                            "Riprova tra qualche secondo o scegli un altro mezzo.");
                    }

                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    if (attempt < MAX_RETRY)
                    {
                        // Ritenta dopo delay esponenziale (50ms, 100ms, 200ms)
                        _logger.LogWarning(
                            "Race condition su mezzo {Matricola}. Tentativo {Attempt}/{MaxRetry}",
                            matricola, attempt, MAX_RETRY);

                        await Task.Delay(50 * (int)Math.Pow(2, attempt - 1));
                        continue; // Torna all'inizio del while
                    }
                    else
                    {
                        // Tentativi esauriti
                        _logger.LogError(ex,
                            "Mezzo {Matricola} non prenotabile dopo {MaxRetry} tentativi",
                            matricola, MAX_RETRY);

                        throw new OperazioneNonConsentitaException(
                            "Il mezzo è stato appena prenotato da un altro utente. Riprova tra qualche secondo.");
                    }
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            throw new OperazioneNonConsentitaException("Errore imprevisto durante la prenotazione. Riprova.");
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
                var durataMinuti = Math.Min(durata.TotalMinutes, 360); 
                decimal costo = Tariffe.COSTO_BASE;

                if (durataMinuti > 30)
                {
                    decimal minutiExtra = (decimal)durataMinuti - 30;
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
                if (costo > 100)
                    costo = 100; //tetto massimo

                _logger.LogInformation("Fine corsa {CorsaId}: durata {DurataMinuti:F1} min, costo {Costo:F2}€, utente {UtenteId}, mezzo {Matricola}",
                    corsaEsistente.Id,
                    durata.TotalMinutes,
                    costo,
                    corsaEsistente.IdUtente,
                    corsaEsistente.MatricolaMezzo
                );


                corsaEsistente.CostoFinale = costo;

                bool problemaSegnalato = dto.SegnalazioneProblema;
                bool batteriaScarica = (mezzo.Tipo == TipoMezzo.MonopattinoElettrico || mezzo.Tipo == TipoMezzo.BiciElettrica)
                                       && mezzo.LivelloBatteria < 20;

                mezzo.Stato = (problemaSegnalato || batteriaScarica)
                    ? StatoMezzo.NonPrelevabile
                    : StatoMezzo.Disponibile;

                //NOTIFICA ADMIN: Mezzo guasto segnalato
                if (problemaSegnalato)
                {
                    try
                    {
                        await _hubContext.Clients.Group("admin")
                            .SendAsync("RiceviNotificaAdmin",
                                "Mezzo Guasto Segnalato",
                                $"Il mezzo {mezzo.Matricola} ({mezzo.Tipo}) è stato segnalato guasto dall'utente {utente.Nome} {utente.Cognome} (ID: {utente.Id})");

                        _logger.LogWarning("Mezzo {Matricola} segnalato guasto da utente {UtenteId}", mezzo.Matricola, utente.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Impossibile inviare notifica guasto mezzo {Matricola}", mezzo.Matricola);
                    }
                }

                //NOTIFICA ADMIN: Batteria bassa
                if (batteriaScarica)
                {
                    try
                    {
                        await _hubContext.Clients.Group("admin")
                            .SendAsync("RiceviNotificaAdmin",
                                "Batteria Critica",
                                $"Il mezzo {mezzo.Matricola} ({mezzo.Tipo}) ha batteria al {mezzo.LivelloBatteria}%. Richiede ricarica urgente.");

                        _logger.LogWarning("Mezzo {Matricola} con batteria bassa: {Livello}%", mezzo.Matricola, mezzo.LivelloBatteria);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Impossibile inviare notifica batteria bassa {Matricola}", mezzo.Matricola);
                    }
                }

                //salva modifiche corsa +  mezzo
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Publish solo dopo commit
                await _mqttIoTService.BloccaMezzoAsync(
                    corsaEsistente.IdParcheggioRilascio ?? 0,
                    corsaEsistente.MatricolaMezzo
                );


                //servizio accede al db per registrare la transazione e aggiornare il credito utente, quindi viene chiamato fuori dalla transazione locale di PutCorsa
                await _pagamentoService.RegistraMovimentoAsync(
                    idUtente: utente.Id,
                    importo: -costo,
                    stato: StatoPagamento.Completato,
                    tipo: "Corsa",
                    idCorsa: corsaEsistente.Id
                );

                if (utente.Credito < 0)
                {
                    await SospendiUtenteAsync(utente, "Credito negativo dopo la corsa");
                }

                /*await _context.RegistraTransazioneAsync(
                    idUtente: utente.Id,
                    importo: -corsaEsistente.CostoFinale.GetValueOrDefault(),
                    stato: StatoPagamento.Completato,
                    idCorsa: corsaEsistente.Id
                );*/

                await _hubContext.Clients.Group($"utenti:{utente.Id}")
                .SendAsync("NuovaTransazione", new
                {
                    Importo = -corsaEsistente.CostoFinale,
                    Tipo = "Corsa",
                    Data = DateTime.Now
                });



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
                    TipoMezzo = mezzo.Tipo.ToString(),
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

        // gestione sospensione utente 
        private async Task SospendiUtenteAsync(Utente utente, string motivo)
        {
            if (!utente.Sospeso)
            {
                utente.Sospeso = true;
                await _context.SaveChangesAsync();

                //Notifica all’utente
                try
                {
                    await _hubContext.Clients.Group($"utenti:{utente.Id}")
                        .SendAsync("UtenteSospeso", new
                        {
                            id = utente.Id,
                            nome = utente.Nome,
                            messaggio = $"Il tuo account è stato sospeso: {motivo}"
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SignalR: impossibile inviare notifica sospensione utente {Id}", utente.Id);
                }

                //Notifica ai gestori
                try
                {
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("RiceviNotificaAdmin", "Utente sospeso",
                            $"L’utente {utente.Nome} (ID {utente.Id}) è stato sospeso. Motivo: {motivo}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SignalR: impossibile notificare admin per sospensione utente {Id}", utente.Id);
                }

                _logger.LogWarning("Utente {Id} sospeso: {Motivo}", utente.Id, motivo);
            }
        }

    }
}
