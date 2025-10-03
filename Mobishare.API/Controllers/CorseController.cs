using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.IoT.Interfaces;
using System.Security.Claims;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorseController(MobishareDbContext context, IMqttIoTService mqttIoTService) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly IMqttIoTService _mqttIoTService = mqttIoTService;


        // GET: api/corse? idUtente=& matricolaMezzo=..
        [Authorize(Roles = "Gestore")]
        [HttpGet]
        public async Task<ActionResult<SuccessResponse>> GetCorse([FromQuery] int? idUtente, [FromQuery] string? matricolaMezzo)
        {
            var query = _context.Corse.AsNoTracking().AsQueryable();

            if (idUtente.HasValue && idUtente.Value > 0)
                query = query.Where(c => c.IdUtente == idUtente.Value);

            if (!string.IsNullOrEmpty(matricolaMezzo))
                query = query.Where(c => c.MatricolaMezzo == matricolaMezzo);

            var corse = await query
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
        [HttpGet("utente/{idUtente}")]
        public async Task<ActionResult<SuccessResponse>> GetStoricoCorseUtente(int idUtente)
        {

            // Verifica che l'utente esista
            var utente = await _context.Utenti.FindAsync(idUtente)
                          ?? throw new ElementoNonTrovatoException("Utente", idUtente);

            // Recupera le corse di quell'utente
            var corse = await _context.Corse
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



        [HttpGet("{id}")]
        public async Task<ActionResult<SuccessResponse>> GetCorsaById(int id)
        {
            var corsa = await _context.Corse.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id)
               ?? throw new ElementoNonTrovatoException("Corsa", id); 

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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Recupero utente dal token JWT
                var idUtente = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? throw new OperazioneNonConsentitaException("Utente non autenticato"));

                var utente = await _context.Utenti.FindAsync(idUtente)
                     ?? throw new ElementoNonTrovatoException("Utente", idUtente);

                var mezzo = await _context.Mezzi.FirstOrDefaultAsync(m => m.Matricola == dto.MatricolaMezzo);

                if (mezzo == null)
                    throw new ElementoNonTrovatoException("Mezzo", dto.MatricolaMezzo);

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

                // Avvia davvero il mezzo tramite MQTT
                await _mqttIoTService.SbloccaMezzoAsync(
                    dto.IdParcheggioPrelievo,
                    dto.MatricolaMezzo,
                    utente.Id.ToString()
                );

                var response = new CorsaResponseDTO
                {
                    Id = corsa.Id,
                    IdUtente = corsa.IdUtente,
                    MatricolaMezzo = corsa.MatricolaMezzo,
                    IdParcheggioPrelievo = corsa.IdParcheggioPrelievo,
                    DataOraInizio = corsa.DataOraInizio
                };

                return CreatedAtAction(nameof(GetCorsaById), new { id = corsa.Id }, new SuccessResponse
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
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new OperazioneNonConsentitaException($"Errore durante la gestione della corsa: {ex.Message}");
            }
        }




        // PUT: api/corse/{id} -> fine corsa + logica credito consistente
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

                // Calcolo costo
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
