using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.Core.DTOs;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.IoT.Interfaces;
using Mobishare.Infrastructure.SignalRHubs;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MezziController(MobishareDbContext context, IMqttIoTService mqttPublisher, ILogger<MezziController> logger, IHubContext<NotificheHub> hubContext) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly IMqttIoTService _mqttIoTService = mqttPublisher;
        private readonly ILogger<MezziController> _logger = logger;
        private readonly IHubContext<NotificheHub> _hubContext = hubContext;


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
                    NomeParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Nome : null,
                    ZonaParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Zona : null,
                    IndirizzoParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Indirizzo : null,
                    MotivoNonPrelevabile = m.MotivoNonPrelevabile != Core.Enums.MotivoNonPrelevabile.Nessuno
                        ? m.MotivoNonPrelevabile.ToString()
                        : null
                })
                .ToListAsync();

            return Ok(new SuccessResponse
            {
                Messaggio = "Lista completa mezzi",
                Dati = mezzi
            });
        }

        // GET: api/mezzi/{id} -> utile in backend/admin/opInterne
        [Authorize(Roles = "Gestore, Utente")]
        [HttpGet("{id}")]
        public async Task<ActionResult<SuccessResponse>> GetMezzo(int id)
        {
            var dto = await _context.Mezzi
                .Where(mezzo => mezzo.Id == id)
                .Select(m => new MezzoResponseDTO
                {
                    Id = m.Id,
                    Matricola = m.Matricola,
                    Tipo = m.Tipo.ToString(),
                    Stato = m.Stato.ToString(),
                    LivelloBatteria = m.LivelloBatteria,
                    IdParcheggioCorrente = m.IdParcheggioCorrente,
                    NomeParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Nome : null,
                    ZonaParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Zona : null,
                    IndirizzoParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Indirizzo : null,
                    MotivoNonPrelevabile = m.MotivoNonPrelevabile != Core.Enums.MotivoNonPrelevabile.Nessuno
                        ? m.MotivoNonPrelevabile.ToString()
                        : null
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
                NomeParcheggio = mezzo.ParcheggioCorrente?.Nome,
                ZonaParcheggio = mezzo.ParcheggioCorrente?.Zona,
                IndirizzoParcheggio = mezzo.ParcheggioCorrente?.Indirizzo,
                MotivoNonPrelevabile = mezzo.MotivoNonPrelevabile != Core.Enums.MotivoNonPrelevabile.Nessuno
                    ? mezzo.MotivoNonPrelevabile.ToString()
                    : null
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
                        : null),// bici muscolare: nessuna batteria
                IdParcheggioCorrente = dto.IdParcheggioCorrente,
            };

            ApplicaRegoleBatteria(mezzo);

            _context.Mezzi.Add(mezzo);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Creato nuovo mezzo {Matricola} di tipo {Tipo} con stato iniziale {Stato} nel parcheggio ID {ParcheggioId}", mezzo.Matricola, mezzo.Tipo, mezzo.Stato, mezzo.IdParcheggioCorrente);

            // mappa entità EF → DTO di risposta
            var response = new MezzoResponseDTO
            {
                Id = mezzo.Id,
                Matricola = mezzo.Matricola,
                Tipo = mezzo.Tipo.ToString(),
                Stato = mezzo.Stato.ToString(),
                LivelloBatteria = mezzo.LivelloBatteria,
                IdParcheggioCorrente = mezzo.IdParcheggioCorrente,
                NomeParcheggio = parcheggio.Nome,
                MotivoNonPrelevabile = mezzo.MotivoNonPrelevabile != Core.Enums.MotivoNonPrelevabile.Nessuno
                    ? mezzo.MotivoNonPrelevabile.ToString()
                    : null
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
                var parcheggio = await _context.Parcheggi.FindAsync(dto.IdParcheggioCorrente.Value) 
                    ?? throw new ElementoNonTrovatoException("Parcheggio", dto.IdParcheggioCorrente);

                mezzo.IdParcheggioCorrente = dto.IdParcheggioCorrente.Value;
                mezzo.ParcheggioCorrente = parcheggio;
            }

            if (mezzo.Stato == StatoMezzo.NonPrelevabile)
            {
                _logger.LogWarning("Mezzo {Matricola} segnalato come guasto (batteria: {Batteria}%)",
                    mezzo.Matricola, mezzo.LivelloBatteria);
            }
            else
            {
                _logger.LogInformation("Stato mezzo {Matricola} aggiornato a {Stato}", mezzo.Matricola, mezzo.Stato);
            }

            await _context.SaveChangesAsync();

            await _mqttIoTService.PublishAsync("mobishare/mezzo/stato", new
            {
                idMezzo = mezzo.Id,
                nuovoStato = mezzo.Stato.ToString(),
                parcheggio = mezzo.ParcheggioCorrente?.Nome,
                livelloBatteria = mezzo.LivelloBatteria,
                timestamp = DateTime.Now,
                source = "API"
            });

            _logger.LogInformation("Stato mezzo {Matricola} aggiornato a {Stato}", mezzo.Matricola, mezzo.Stato);

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


        // PUT: api/mezzi/{id}/ricarica
        [Authorize(Roles = "Gestore")]
        [HttpPut("{id}/ricarica")]
        public async Task<IActionResult> RicaricaMezzo(int id)
        {
            var mezzo = await _context.Mezzi.FindAsync(id)
                ?? throw new ElementoNonTrovatoException("Mezzo", id);

            // solo per mezzi elettrici
            if (mezzo.Tipo != TipoMezzo.BiciElettrica && mezzo.Tipo != TipoMezzo.MonopattinoElettrico)
                return BadRequest(new { errore = "Solo i mezzi elettrici possono essere ricaricati." });

            mezzo.LivelloBatteria = 100;
            mezzo.Stato = StatoMezzo.Disponibile;
            mezzo.MotivoNonPrelevabile = Core.Enums.MotivoNonPrelevabile.Nessuno;

            await _context.SaveChangesAsync();

            await _mqttIoTService.PublishAsync("mobishare/mezzo/ricarica", new
            {
                idMezzo = mezzo.Id,
                mezzo.Matricola,
                livelloBatteria = mezzo.LivelloBatteria,
                stato = mezzo.Stato.ToString(),
                timestamp = DateTime.Now,
                source = "API"
            });

            return Ok(new SuccessResponse
            {
                Messaggio = $"Il mezzo {mezzo.Matricola} è stato ricaricato e reso disponibile.",
                Dati = new
                {
                    mezzo.Id,
                    mezzo.Matricola,
                    mezzo.LivelloBatteria,
                    nuovoStato = mezzo.Stato.ToString()
                }
            });
        }


        // PUT: api/mezzi/matricola/{matricola}/segnala-guasto
        [Authorize]
        [HttpPut("matricola/{matricola}/segnala-guasto")]
        public async Task<IActionResult> SegnalaGuastoByMatricola(string matricola)
        {
            var mezzo = await _context.Mezzi
                .FirstOrDefaultAsync(m => m.Matricola == matricola);

            if (mezzo == null)
                return NotFound(new { Messaggio = $"Mezzo con matricola {matricola} non trovato." });

            // Se è già segnalato, evita di ripetere
            if (mezzo.Stato == StatoMezzo.NonPrelevabile)
            {
                return Ok(new SuccessResponse
                {
                    Messaggio = "Il mezzo è già segnalato come guasto.",
                    Dati = new
                    {
                        mezzo.Id,
                        mezzo.Matricola,
                        stato = mezzo.Stato.ToString()
                    }
                });
            }

            mezzo.Stato = StatoMezzo.NonPrelevabile;
            mezzo.MotivoNonPrelevabile = Core.Enums.MotivoNonPrelevabile.GuastoSegnalato; 
            await _context.SaveChangesAsync();

            _logger.LogWarning("Segnalato guasto per mezzo {Matricola}", mezzo.Matricola);

            // Pubblica aggiornamento su MQTT 
            await _mqttIoTService.PublishAsync("mobishare/mezzo/stato", new
            {
                idMezzo = mezzo.Id,
                matricola = mezzo.Matricola,
                stato = mezzo.Stato.ToString(),
                motivo = "Guasto segnalato dall’utente",
                timestamp = DateTime.Now,
                source = "API"
            });

            //Notifica SignalR Admin
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdClaim, out int userId))
                {
                    var utente = await _context.Utenti.FindAsync(userId);

                    await _hubContext.Clients.Group("admin")
                        .SendAsync("RiceviNotificaAdmin",
                            "Mezzo Guasto Segnalato",
                            $"Il mezzo {mezzo.Matricola} ({mezzo.Tipo}) è stato segnalato guasto dall'utente {utente?.Nome} {utente?.Cognome} (ID: {userId}).");

                    _logger.LogInformation("Notifica guasto inviata agli admin per mezzo {Matricola} da utente {UserId}", mezzo.Matricola, userId);
                }
                else
                {
                    // Notifica senza info utente
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("RiceviNotificaAdmin",
                            "Mezzo Guasto Segnalato",
                            $"Il mezzo {mezzo.Matricola} ({mezzo.Tipo}) è stato segnalato come guasto.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore invio notifica SignalR per guasto mezzo {Matricola}", mezzo.Matricola);
            }

            return Ok(new SuccessResponse
            {
                Messaggio = $"Mezzo {mezzo.Matricola} segnalato come guasto con successo.",
                Dati = new
                {
                    mezzo.Id,
                    mezzo.Matricola,
                    nuovoStato = mezzo.Stato.ToString()
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
                    NomeParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Nome : null,
                    ZonaParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Zona : null,
                    IndirizzoParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Indirizzo : null
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
            if (mezzo.MotivoNonPrelevabile != Core.Enums.MotivoNonPrelevabile.Nessuno)
            {
                return mezzo.MotivoNonPrelevabile switch
                {
                    Core.Enums.MotivoNonPrelevabile.GuastoSegnalato => "Guasto segnalato dall'utente",
                    Core.Enums.MotivoNonPrelevabile.BatteriaScarica => "Batteria scarica",
                    _ => mezzo.MotivoNonPrelevabile.ToString()
                };
            }

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
            var diff = Math.Abs((vecchioLivello ?? 0) - (mezzo.LivelloBatteria ?? 0));
            if (diff >= 5 || vecchioStato != mezzo.Stato)
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
                    timestamp = DateTime.Now,
                    source = "API"
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
                    NomeParcheggio = m.ParcheggioCorrente != null ? m.ParcheggioCorrente.Nome : null,
                    MotivoNonPrelevabile = m.MotivoNonPrelevabile != Core.Enums.MotivoNonPrelevabile.Nessuno
                        ? m.MotivoNonPrelevabile.ToString()
                        : null
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
            if (mezzo.Tipo == TipoMezzo.BiciMuscolare)
            {
                mezzo.LivelloBatteria = null;
                return;
            }

            if (mezzo.Tipo == TipoMezzo.BiciElettrica || mezzo.Tipo == TipoMezzo.MonopattinoElettrico)
            {
                // Se batteria scarica → NonPrelevabile con motivo BatteriaScarica
                if (mezzo.LivelloBatteria < 20 && mezzo.Stato == StatoMezzo.Disponibile)
                {
                    mezzo.Stato = StatoMezzo.NonPrelevabile;
                    mezzo.MotivoNonPrelevabile = Core.Enums.MotivoNonPrelevabile.BatteriaScarica;
                }
                // Se batteria OK E motivo era batteria scarica → torna Disponibile
                else if (mezzo.LivelloBatteria >= 20
                         && mezzo.Stato == StatoMezzo.NonPrelevabile
                         && mezzo.MotivoNonPrelevabile == Core.Enums.MotivoNonPrelevabile.BatteriaScarica)
                {
                    mezzo.Stato = StatoMezzo.Disponibile;
                    mezzo.MotivoNonPrelevabile = Core.Enums.MotivoNonPrelevabile.Nessuno;
                }
            }
        }
    }
}
