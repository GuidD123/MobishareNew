using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.PhilipsHue;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParcheggiController(MobishareDbContext context, ILogger<MezziController> logger, PhilipsHueControl philipsHueControl) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly ILogger<MezziController> _logger = logger;
        private readonly PhilipsHueControl _philipsHueControl = philipsHueControl;


        // ZONA UTENTE 
        // GET: api/parcheggi
        [HttpGet]
        public async Task<ActionResult<SuccessResponse>> GetParcheggi()
        {
            var parcheggi = await _context.Parcheggi
                .Include(p => p.Mezzi)
                .AsNoTracking()
                .ToListAsync();

            //carico i mezzi separatamente
            var result = parcheggi.Select(p => new ParcheggioResponseDTO
            {
                Id = p.Id,
                Nome = p.Nome,
                Zona = p.Zona,
                Indirizzo = p.Indirizzo,
                Capienza = p.Capienza,
                Attivo = p.Attivo,
                Mezzi = p.Mezzi.Select(m => new MezzoResponseDTO
                {
                    Id = m.Id,
                    Matricola = m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    Stato = m.Stato.ToString(),
                    LivelloBatteria = m.LivelloBatteria,
                    IdParcheggioCorrente = m.IdParcheggioCorrente,
                    NomeParcheggio = p.Nome,
                    MotivoNonPrelevabile = m.MotivoNonPrelevabile != Core.Enums.MotivoNonPrelevabile.Nessuno
                        ? m.MotivoNonPrelevabile.ToString()
                        : null
                }).ToList()
            }).ToList();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista parcheggi",
                Dati = result
            });
        }

        // GET: api/parcheggi/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SuccessResponse>> GetParcheggio(int id)
        {
            var parcheggio = await _context.Parcheggi
                .Include(p => p.Mezzi)   // carico anche i mezzi collegati
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parcheggio == null)
                throw new ElementoNonTrovatoException("Parcheggio", id);

            var dto = new ParcheggioResponseDTO
            {
                Id = parcheggio.Id,
                Nome = parcheggio.Nome,
                Zona = parcheggio.Zona,
                Indirizzo = parcheggio.Indirizzo,
                Capienza = parcheggio.Capienza,
                Attivo = parcheggio.Attivo,
                Mezzi = parcheggio.Mezzi.Select(m => new MezzoResponseDTO
                {
                    Id = m.Id,
                    Matricola = m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    Stato = m.Stato.ToString(),
                    LivelloBatteria = m.LivelloBatteria,
                    IdParcheggioCorrente = m.IdParcheggioCorrente,
                    NomeParcheggio = parcheggio.Nome,
                    MotivoNonPrelevabile = m.MotivoNonPrelevabile != Core.Enums.MotivoNonPrelevabile.Nessuno
                        ? m.MotivoNonPrelevabile.ToString()
                        : null
                }).ToList()
            };

            return Ok(new SuccessResponse
            {
                Messaggio = "Dettaglio parcheggio",
                Dati = dto
            });
        }

        // GET: api/parcheggi/{id}/disponibilita
        [HttpGet("{id}/disponibilita")]
        public async Task<ActionResult<SuccessResponse>> GetDisponibilitaParcheggio(int id)
        {
            var parcheggio = await _context.Parcheggi
                .Include(p => p.Mezzi)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parcheggio == null)
                throw new ElementoNonTrovatoException("Parcheggio", id);

            // mezzi che contano come "occupanti posti"
            var occupati = parcheggio.Mezzi.Count(m =>
                m.Stato != StatoMezzo.InUso &&
                m.Stato != StatoMezzo.Manutenzione);

            var liberi = parcheggio.Capienza - occupati;

            return Ok(new SuccessResponse
            {
                Messaggio = $"Disponibilità parcheggio {parcheggio.Nome}",
                Dati = new
                {
                    postiTotali = parcheggio.Capienza,
                    postiOccupati = occupati,
                    postiLiberi = liberi
                }
            });
        }




        // ZONA ADMIN
        // POST: api/parcheggi -> Aggiungi un nuovo parcheggio
        [HttpPost]
        public async Task<ActionResult<SuccessResponse>> PostParcheggio([FromBody] ParcheggioCreateDTO dto)
        {
            if (await _context.Parcheggi.AnyAsync(p => p.Nome == dto.Nome))
                throw new ElementoDuplicatoException("Parcheggio", dto.Nome);

            var parcheggio = new Parcheggio
            {
                Nome = dto.Nome,
                Zona = dto.Zona,
                Indirizzo = dto.Indirizzo,
                Capienza = dto.Capienza,
                Attivo = dto.Attivo
            };

            _context.Parcheggi.Add(parcheggio);
            await _context.SaveChangesAsync();

            var response = new ParcheggioResponseDTO
            {
                Id = parcheggio.Id,
                Nome = parcheggio.Nome,
                Zona = parcheggio.Zona,
                Indirizzo = parcheggio.Indirizzo,
                Capienza = parcheggio.Capienza,
                Attivo = parcheggio.Attivo,
                Mezzi = [] 
            };

            return CreatedAtAction(nameof(GetParcheggio), new { id = parcheggio.Id }, new SuccessResponse
            {
                Messaggio = "Parcheggio creato correttamente",
                Dati = response
            });
        }

        [HttpPut("{id}/stato")]
        public async Task<ActionResult<SuccessResponse>> AggiornaStatoParcheggio(int id, [FromBody] ParcheggioStatoDTO dto)
        {
            var parcheggio = await _context.Parcheggi
                .Include(p => p.Mezzi)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parcheggio == null)
                throw new ElementoNonTrovatoException("Parcheggio", id);

            parcheggio.Attivo = dto.Attivo;

            if (!dto.Attivo)
            {
                // Cerca altri parcheggi attivi
                var parcheggiAttivi = await _context.Parcheggi
                    .Include(p => p.Mezzi)
                    .Where(p => p.Attivo && p.Id != parcheggio.Id)
                    .Select(p => new
                    {
                        Parcheggio = p,
                        Carico = p.Mezzi.Count
                    })
                    .ToListAsync();

                if (parcheggiAttivi.Any())
                {
                    // Ordina per carico crescente
                    var ordinati = parcheggiAttivi.OrderBy(pa => pa.Carico).ToList();
                    int index = 0;
                    int totaleDest = ordinati.Count;

                    foreach (var mezzo in parcheggio.Mezzi)
                    {
                        var destinazione = ordinati[index % totaleDest].Parcheggio;

                        // Verifica capienza
                        if (destinazione.Mezzi.Count < destinazione.Capienza)
                        {
                            mezzo.IdParcheggioCorrente = destinazione.Id;
                            mezzo.Stato = StatoMezzo.Disponibile;
                            destinazione.Mezzi.Add(mezzo);

                            ordinati[index % totaleDest] = new
                            {
                                Parcheggio = destinazione,
                                Carico = destinazione.Mezzi.Count
                            };
                        }
                        else
                        {
                            // Nessun posto libero, metti in manutenzione
                            mezzo.Stato = StatoMezzo.Manutenzione;
                            
                            // === PHILIPS HUE: Luce GIALLA (manutenzione) ===
                            try
                            {
                                await _philipsHueControl.SetSpiaColor(mezzo.Matricola, ColoreSpia.Giallo);
                                _logger.LogInformation(
                                    "💡 Philips Hue: Luce {Matricola} → GIALLO (Manutenzione - parcheggio pieno)",
                                    mezzo.Matricola);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Impossibile aggiornare luce Philips Hue per mezzo {Matricola}", mezzo.Matricola);
                            }
                        }

                        index++;
                    }
                }
                else
                {
                    // Nessun parcheggio attivo → tutti in manutenzione
                    foreach (var mezzo in parcheggio.Mezzi)
                    {
                        mezzo.Stato = StatoMezzo.Manutenzione;
                        
                        // === PHILIPS HUE: Luce GIALLA (manutenzione) ===
                        try
                        {
                            await _philipsHueControl.SetSpiaColor(mezzo.Matricola, ColoreSpia.Giallo);
                            _logger.LogInformation(
                                "💡 Philips Hue: Luce {Matricola} → GIALLO (Manutenzione - no parcheggi attivi)",
                                mezzo.Matricola);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Impossibile aggiornare luce Philips Hue per mezzo {Matricola}", mezzo.Matricola);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = dto.Attivo
                    ? $"Parcheggio {parcheggio.Nome} riattivato correttamente."
                    : $"Parcheggio {parcheggio.Nome} disattivato: mezzi spostati verso parcheggi meno carichi o messi in manutenzione.",
                Dati = new
                {
                    parcheggio.Id,
                    parcheggio.Nome,
                    parcheggio.Attivo,
                    TotaleMezziCoinvolti = parcheggio.Mezzi.Count
                }
            });
        }

        // DELETE: api/parcheggi/{id}
        [Authorize(Roles = "Gestore")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteParcheggio(int id)
        {
            var parcheggio = await _context.Parcheggi
                .Include(p => p.Mezzi)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new ElementoNonTrovatoException("Parcheggio", id);

            //Verifica che il parcheggio sia vuoto
            if (parcheggio.Mezzi.Any())
            {
                throw new OperazioneNonConsentitaException(
                    $"Impossibile eliminare: il parcheggio contiene {parcheggio.Mezzi.Count} mezzi. " +
                    "Sposta prima i mezzi in altri parcheggi.");
            }

            //Verifica che non sia referenziato in corse come prelievo
            var haCorsePrelievo = await _context.Corse
                .AnyAsync(c => c.IdParcheggioPrelievo == id);

            if (haCorsePrelievo)
            {
                throw new OperazioneNonConsentitaException(
                    "Impossibile eliminare: il parcheggio è referenziato in corse storiche come punto di prelievo. " +
                    "L'eliminazione comprometterebbe l'integrità dei dati.");
            }

            //Verifica che non sia referenziato in corse come rilascio
            var haCorseRilascio = await _context.Corse
                .AnyAsync(c => c.IdParcheggioRilascio == id);

            if (haCorseRilascio)
            {
                throw new OperazioneNonConsentitaException(
                    "Impossibile eliminare: il parcheggio è referenziato in corse storiche come punto di rilascio. " +
                    "L'eliminazione comprometterebbe l'integrità dei dati.");
            }


            var mezziDaSpostare = parcheggio.Mezzi.ToList();
            int mezziSpostati = 0;
            int mezziInManutenzione = 0;

            //ridistribuzione mezzi automaticamente
            if (mezziDaSpostare.Any())
            {
                // Cerca parcheggi attivi con spazio disponibile
                var parcheggiAttivi = await _context.Parcheggi
                    .Include(p => p.Mezzi)
                    .Where(p => p.Attivo && p.Id != parcheggio.Id)
                    .Select(p => new
                    {
                        Parcheggio = p,
                        PostiLiberi = p.Capienza - p.Mezzi.Count
                    })
                    .Where(p => p.PostiLiberi > 0)
                    .OrderByDescending(p => p.PostiLiberi) // Priorità ai più vuoti
                    .ToListAsync();

                if (parcheggiAttivi.Count != 0)
                {
                    int index = 0;
                    foreach (var mezzo in mezziDaSpostare)
                    {
                        var destinazione = parcheggiAttivi[index % parcheggiAttivi.Count].Parcheggio;

                        // Verifica capienza
                        if (destinazione.Mezzi.Count < destinazione.Capienza)
                        {
                            mezzo.IdParcheggioCorrente = destinazione.Id;
                            mezzo.Stato = StatoMezzo.Disponibile;
                            mezziSpostati++;

                            _logger.LogInformation(
                                "Mezzo {Matricola} spostato da parcheggio {ParcheggioOld} a {ParcheggioNew}",
                                mezzo.Matricola, parcheggio.Nome, destinazione.Nome);
                        }
                        else
                        {
                            // Parcheggio pieno, vai al prossimo
                            index++;
                            if (index >= parcheggiAttivi.Count)
                            {
                                // Nessun posto libero rimasto
                                mezzo.Stato = StatoMezzo.Manutenzione;
                                //mezzo.IdParcheggioCorrente = null; // ← IMPORTANTE
                                mezziInManutenzione++;
                                
                                // === PHILIPS HUE: Luce GIALLA (manutenzione) ===
                                try
                                {
                                    await _philipsHueControl.SetSpiaColor(mezzo.Matricola, ColoreSpia.Giallo);
                                    _logger.LogInformation(
                                        "💡 Philips Hue: Luce {Matricola} → GIALLO (Manutenzione - no posti liberi)",
                                        mezzo.Matricola);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Impossibile aggiornare luce Philips Hue per mezzo {Matricola}", mezzo.Matricola);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Nessun parcheggio attivo disponibile
                    foreach (var mezzo in mezziDaSpostare)
                    {
                        mezzo.Stato = StatoMezzo.Manutenzione;
                        //mezzo.IdParcheggioCorrente = null;
                        mezziInManutenzione++;
                        
                        // === PHILIPS HUE: Luce GIALLA (manutenzione) ===
                        try
                        {
                            await _philipsHueControl.SetSpiaColor(mezzo.Matricola, ColoreSpia.Giallo);
                            _logger.LogInformation(
                                "💡 Philips Hue: Luce {Matricola} → GIALLO (Manutenzione - eliminazione parcheggio)",
                                mezzo.Matricola);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Impossibile aggiornare luce Philips Hue per mezzo {Matricola}", mezzo.Matricola);
                        }
                    }
                }

                await _context.SaveChangesAsync(); // Salva spostamenti mezzi
            }

            //Eliminazione
            _context.Parcheggi.Remove(parcheggio);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Parcheggio {Nome} (ID {Id}) eliminato. Mezzi spostati: {Spostati}, in manutenzione: {Manutenzione}",
                parcheggio.Nome, parcheggio.Id, mezziSpostati, mezziInManutenzione);

            return Ok(new SuccessResponse
            {
                Messaggio = $"Parcheggio '{parcheggio.Nome}' eliminato con successo",
                Dati = new
                {
                    id = parcheggio.Id,
                    nome = parcheggio.Nome,
                    zona = parcheggio.Zona,
                    mezziGestiti = new
                    {
                        totale = mezziDaSpostare.Count,
                        spostati = mezziSpostati,
                        inManutenzione = mezziInManutenzione
                    }
                }
            });
        }


        public class ParcheggioStatoDTO
        {
            public bool Attivo { get; set; }
        }

    }
}
