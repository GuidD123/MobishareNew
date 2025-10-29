using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using Mobishare.Core.DTOs;
using Mobishare.Infrastructure.IoT.Interfaces;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MezziController(MobishareDbContext context, IMqttIoTService mqttPublisher) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly IMqttIoTService _mqttIoTService = mqttPublisher;


        // GET: api/mezzi -> serve all'admin per vedere tutti i mezzi della flotta (disponibili, occupati e guasti) 
        [Authorize(Roles = "Gestore")]
        [HttpGet]
        public async Task<ActionResult<SuccessResponse>> GetMezzi()
        {
            var mezzi = await _context.Mezzi
                .Include(m => m.ParcheggioCorrente)
                .Select(m => new MezzoResponseDTO
                {
                    Id = m.Id,
                    Matricola = m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    Stato = m.Stato.ToString(),
                    LivelloBatteria = m.LivelloBatteria,
                    IdParcheggioCorrente = m.IdParcheggioCorrente,
                    NomeParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Nome : null
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista completa mezzi",
                Dati = mezzi
            });
        }



        // GET: api/mezzi/{id} -> utile in backend/admin/opInterne
        [HttpGet("{id}")]
        public async Task<ActionResult<SuccessResponse>> GetMezzo(int id)
        {
            var dto = await _context.Mezzi
                .Where(m => m.Id == id)
                .Select(m => new MezzoResponseDTO
                {
                    Id = m.Id,
                    Matricola = m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    Stato = m.Stato.ToString(),
                    LivelloBatteria = m.LivelloBatteria,
                    IdParcheggioCorrente = m.IdParcheggioCorrente,
                    NomeParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Nome : null
                })
                .FirstOrDefaultAsync() ?? throw new ElementoNonTrovatoException("Mezzo", id);

            return Ok(new SuccessResponse
            {
                Messaggio = "Dettaglio mezzo",
                Dati = dto
            });
        }


        // GET: api/mezzi/matricola/{matricola} -> unico modo per utente di vedere mezzo 
        [HttpGet("matricola/{matricola}")]
        public async Task<ActionResult<SuccessResponse>> GetMezzoByMatricola(string matricola)
        {
            var mezzo = await _context.Mezzi
                .Include(m => m.ParcheggioCorrente)
                .FirstOrDefaultAsync(m => m.Matricola == matricola);

            if (mezzo == null)
                throw new ElementoNonTrovatoException("Mezzo", matricola);

            var dto = new MezzoResponseDTO
            {
                Id = mezzo.Id,
                Matricola = mezzo.Matricola,
                Tipo = mezzo.Tipo.ToString(),
                Stato = mezzo.Stato.ToString(),
                LivelloBatteria = mezzo.LivelloBatteria,
                IdParcheggioCorrente = mezzo.IdParcheggioCorrente,
                NomeParcheggio = mezzo.ParcheggioCorrente?.Nome
            };

            return Ok(new SuccessResponse
            {
                Messaggio = "Mezzo trovato",
                Dati = dto
            });
        }


        // POST: api/mezzi -> crea mezzo
        [Authorize(Roles = "Gestore")]
        [HttpPost]
        public async Task<ActionResult<SuccessResponse>> PostMezzo([FromBody] MezzoCreateDTO dto)
        {
            if (await _context.Mezzi.AnyAsync(m => m.Matricola == dto.Matricola))
                throw new ElementoDuplicatoException("Mezzo", dto.Matricola);

            //Verifica esistenza parcheggio 
            var parcheggio = await _context.Parcheggi.FindAsync(dto.IdParcheggioCorrente) ?? throw new ElementoNonTrovatoException("Parcheggio", dto.IdParcheggioCorrente);

            //Business rule - mezzi elettrici con batteria bassa non possono essere disponibili
            if ((dto.Tipo == TipoMezzo.BiciElettrica || dto.Tipo == TipoMezzo.MonopattinoElettrico)
                && dto.LivelloBatteria < 20 && dto.Stato == StatoMezzo.Disponibile)
                throw new BatteriaTroppoBassaException();

            // mappa DTO → entità EF
            var mezzo = new Mezzo
            {
                Matricola = dto.Matricola,
                Tipo = dto.Tipo,
                Stato = dto.Stato,
                LivelloBatteria = dto.LivelloBatteria > 0
                    ? dto.LivelloBatteria
                    : (dto.Tipo == TipoMezzo.BiciElettrica || dto.Tipo == TipoMezzo.MonopattinoElettrico
                        ? Random.Shared.Next(20, 101) // batteria random 20-100
                        : 100), // bici muscolare sempre 100
                IdParcheggioCorrente = dto.IdParcheggioCorrente
            };

            ApplicaRegoleBatteria(mezzo);

            _context.Mezzi.Add(mezzo);
            await _context.SaveChangesAsync();

            // mappa entità EF → DTO di risposta
            var response = new MezzoResponseDTO
            {
                Id = mezzo.Id,
                Matricola = mezzo.Matricola,
                Tipo = mezzo.Tipo.ToString(),
                Stato = mezzo.Stato.ToString(),
                LivelloBatteria = mezzo.LivelloBatteria,
                IdParcheggioCorrente = mezzo.IdParcheggioCorrente,
                NomeParcheggio = parcheggio.Nome
            };

            return CreatedAtAction(nameof(GetMezzo), new { id = mezzo.Id }, new SuccessResponse
            {
                Messaggio = "Mezzo creato correttamente",
                Dati = response
            });

        }


        //PUT: api/mezzi/{id} -> cambia lo stato del mezzo e il parcheggio corrente
        [Authorize(Roles = "Gestore")]
        [HttpPut("{id}/stato")]
        public async Task<IActionResult> CambiaStatoMezzo(int id, [FromBody] MezzoUpdateDTO dto)
        {
            // Trova il mezzo
            var mezzo = await _context.Mezzi.FindAsync(id) ?? throw new ElementoNonTrovatoException("Mezzo", id);

            // Applica aggiornamenti
            mezzo.LivelloBatteria = dto.LivelloBatteria;
            mezzo.Stato = dto.Stato;

            ApplicaRegoleBatteria(mezzo);

            // Se viene cambiato il parcheggio, verifica che esista
            if (dto.IdParcheggioCorrente.HasValue)
            {
                var parcheggio = await _context.Parcheggi.FindAsync(dto.IdParcheggioCorrente.Value) ?? throw new ElementoNonTrovatoException("Parcheggio", dto.IdParcheggioCorrente);

                mezzo.IdParcheggioCorrente = dto.IdParcheggioCorrente.Value;
                mezzo.ParcheggioCorrente = parcheggio;
            }

            await _context.SaveChangesAsync();

            await _mqttIoTService.PublishAsync("mobishare/mezzo/stato", new
            {
                idMezzo = mezzo.Id,
                nuovoStato = mezzo.Stato.ToString(),
                parcheggio = mezzo.ParcheggioCorrente?.Nome,
                livelloBatteria = mezzo.LivelloBatteria,
                timestamp = DateTime.Now
            });


            return Ok(new SuccessResponse
            {
                Messaggio = "Stato del mezzo aggiornato correttamente",
                Dati = new
                {
                    nuovoStato = mezzo.Stato.ToString(),
                    livelloBatteria = mezzo.LivelloBatteria,
                    parcheggio = mezzo.ParcheggioCorrente?.Nome
                }
            });
        }


        //GET: api/mezzi/disponibili
        [Authorize] // accesso consentito a Utente e Gestore
        [HttpGet("disponibili")]
        public async Task<ActionResult<SuccessResponse>> GetMezziDisponibili()
        {
            var mezzi = await _context.Mezzi
                .Include(m => m.ParcheggioCorrente)
                .Where(m => m.Stato == StatoMezzo.Disponibile)
                .Select(m => new MezzoResponseDTO
                {
                    Id = m.Id,
                    Matricola = m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    Stato = m.Stato.ToString(),
                    LivelloBatteria = m.LivelloBatteria,
                    IdParcheggioCorrente = m.IdParcheggioCorrente,
                    NomeParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Nome : null
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista mezzi disponibili",
                Dati = new
                {
                    totale = mezzi.Count,
                    mezzi
                }
            });
        }


        // GET: api/mezzi/nonprelevabili?idUtente=99
        [Authorize(Roles = "Gestore")]  
        [HttpGet("nonprelevabili")]
        public async Task<IActionResult> GetMezziNonPrelevabili()
        {

            var mezzi = await _context.Mezzi
                .Include(m => m.ParcheggioCorrente)
                .Where(m => m.Stato == StatoMezzo.NonPrelevabile)
                .Select(m => new
                {
                    m.Id,
                    m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    m.LivelloBatteria,
                    NomeParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Nome : null,
                    MotivoNonPrelevabile = GetMotivoNonPrelevabile(m)
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista mezzi non prelevabili",
                Dati = new
                {
                    totaleMezziNonPrelevabili = mezzi.Count,
                    mezzi
                }
            });
        }

        // Metodo helper per dedurre il motivo per cui un mezzo è non prelevabile
        private static string GetMotivoNonPrelevabile(Mezzo mezzo)
        {
            if ((mezzo.Tipo == TipoMezzo.BiciElettrica || mezzo.Tipo == TipoMezzo.MonopattinoElettrico)
                && mezzo.LivelloBatteria < 20)
                return "Batteria scarica";

            return "Motivo non specificato";
        }



        // POST: api/mezzi/mqtt-batteria -> simula aggiornamento livello batteria tramite MQTT
        [HttpPost("mqtt-batteria")]
        public async Task<IActionResult> SimulaMQTTBatteria([FromBody] BatteriaDTO input)
        {
            //validazione input
            if (!ModelState.IsValid)
                return BadRequest(ModelState); 

            var mezzo = await _context.Mezzi.FindAsync(input.IdMezzo) ?? throw new ElementoNonTrovatoException("Mezzo", input.IdMezzo);

            //salvo vecchi valori per log 
            var vecchioLivello = mezzo.LivelloBatteria;
            var vecchioStato = mezzo.Stato;

            //aggiorno livello batteria
            mezzo.LivelloBatteria = input.LivelloBatteria;

            //mezzi elettrici
            ApplicaRegoleBatteria(mezzo);

            // Salva modifiche
            await _context.SaveChangesAsync();

            //PUBLISH
            //Pubblica solo se c'è un cambiamento significativo
            if (Math.Abs(vecchioLivello - mezzo.LivelloBatteria) >= 5 || vecchioStato != mezzo.Stato)
            {
                await _mqttIoTService.PublishAsync("mobishare/mezzo/telemetria", new
                {
                    idMezzo = mezzo.Id,
                    matricola = mezzo.Matricola,
                    livelloBatteria = mezzo.LivelloBatteria,
                    stato = mezzo.Stato.ToString(),
                    cambiamento = new
                    {
                        batteria = $"{vecchioLivello}% → {mezzo.LivelloBatteria}%",
                        stato = vecchioStato != mezzo.Stato ? $"{vecchioStato} → {mezzo.Stato}" : "invariato"
                    },
                    timestamp = DateTime.Now
                });
            }

            return Ok(new SuccessResponse
            {
                Messaggio = "Livello batteria aggiornato",
                Dati = new
                {
                    mezzo.Matricola,
                    livelloBatteria = new
                    {
                        precedente = vecchioLivello,
                        attuale = mezzo.LivelloBatteria
                    },
                    stato = new
                    {
                        precedente = vecchioStato.ToString(),
                        attuale = mezzo.Stato.ToString(),
                        cambiato = vecchioStato != mezzo.Stato
                    }
                }
            });
        }

        // GET: api/mezzi/parcheggio/{idParcheggio}
        [HttpGet("parcheggio/{idParcheggio}")]
        public async Task<IActionResult> GetMezziPerParcheggio(int idParcheggio)
        {
            // Verifica esistenza parcheggio
            var parcheggio = await _context.Parcheggi.FindAsync(idParcheggio) ?? throw new ElementoNonTrovatoException("Parcheggio", idParcheggio);

            // Recupera mezzi nel parcheggio
            var mezzi = await _context.Mezzi
                .Where(m => m.IdParcheggioCorrente == idParcheggio)
                .Select(m => new MezzoResponseDTO
                {
                    Id = m.Id,
                    Matricola = m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    Stato = m.Stato.ToString(),
                    LivelloBatteria = m.LivelloBatteria,
                    IdParcheggioCorrente = m.IdParcheggioCorrente,
                    NomeParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Nome : null
                })
                .ToListAsync();

            // Conta mezzi per tipo
            var conta = await _context.Mezzi
                .Where(m => m.IdParcheggioCorrente == idParcheggio)
                .GroupBy(m => m.Tipo)
                .Select(g => new {
                    Tipo = g.Key.ToString(),
                    Quantità = g.Count(),
                    Disponibili = g.Count(m => m.Stato == StatoMezzo.Disponibile),
                    InUso = g.Count(m => m.Stato == StatoMezzo.InUso),
                    NonPrelevabili = g.Count(m => m.Stato == StatoMezzo.NonPrelevabile)
                })
                .ToListAsync();


            return Ok(new SuccessResponse
            {
                Messaggio = $"Mezzi nel parcheggio {parcheggio.Nome}",
                Dati = new
                {
                    parcheggio = parcheggio.Nome,
                    totaleMezzi = mezzi.Count,
                    mezzi,
                    riepilogo = conta
                }
            });
        }



        // GET: api/mezzi/statistiche -> endpoint statistiche mezzi
        [HttpGet("statistiche")]
        public async Task<IActionResult> GetStatisticheMezzi()
        {
            var statistiche = await _context.Mezzi
                .GroupBy(m => m.Stato)
                .Select(g => new
                {
                    Stato = g.Key.ToString(),
                    Conteggio = g.Count()
                })
                .ToListAsync();

            var statisticheTipo = await _context.Mezzi
                .GroupBy(m => m.Tipo)
                .Select(g => new
                {
                    Tipo = g.Key.ToString(),
                    Conteggio = g.Count(),
                    MediaBatteria = g.Where(m => m.Tipo == TipoMezzo.BiciElettrica || m.Tipo == TipoMezzo.MonopattinoElettrico)
                                     .Average(m => (double?)m.LivelloBatteria) ?? 0
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Statistiche sui mezzi",
                Dati = new
                {
                    statistichePerStato = statistiche,
                    statistichePerTipo = statisticheTipo,
                    totale = await _context.Mezzi.CountAsync()
                }
            });
        }

        //Metodo helper per logica batteria 
        private static void ApplicaRegoleBatteria(Mezzo mezzo)
        {
            if (mezzo.Tipo == TipoMezzo.BiciElettrica || mezzo.Tipo == TipoMezzo.MonopattinoElettrico)
            {
                if (mezzo.LivelloBatteria < 20 && mezzo.Stato == StatoMezzo.Disponibile)
                {
                    mezzo.Stato = StatoMezzo.NonPrelevabile;
                }
                else if (mezzo.LivelloBatteria >= 20 && mezzo.Stato == StatoMezzo.NonPrelevabile)
                {
                    mezzo.Stato = StatoMezzo.Disponibile;
                }
            }
        }

    }
}
