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
using Mobishare.Infrastructure.PhilipsHue;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MezziController(MobishareDbContext context, IMqttIoTService mqttPublisher, ILogger<MezziController> logger, IHubContext<NotificheHub> hubContext, PhilipsHueControl philipsHueControl) : ControllerBase
    {
        private readonly MobishareDbContext _context = context;
        private readonly IMqttIoTService _mqttIoTService = mqttPublisher;
        private readonly ILogger<MezziController> _logger = logger;
        private readonly IHubContext<NotificheHub> _hubContext = hubContext;
        private readonly PhilipsHueControl _philipsHueControl = philipsHueControl;


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

        //GET: api/mezzi/matricola/{matricola} -> unico modo per utente di vedere mezzo 
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

        //POST: api/mezzi -> crea mezzo
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
            var mezzo = await _context.Mezzi.FindAsync(id) 
                ?? throw new ElementoNonTrovatoException("Mezzo", id);

            // Applica aggiornamenti
            mezzo.LivelloBatteria = dto.LivelloBatteria;
            mezzo.Stato = dto.Stato;

            // Se il gestore imposta manualmente Disponibile, resetta il motivo
            if (dto.Stato == StatoMezzo.Disponibile)
            {
                mezzo.MotivoNonPrelevabile = MotivoNonPrelevabile.Nessuno;
            }

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

            var coloreSpia = GetColoreSpiaPerStato(mezzo.Stato);

            // === PHILIPS HUE: Cambia colore spia in base allo stato ===
            try
            {
                await _philipsHueControl.SetSpiaColor(mezzo.Matricola, coloreSpia);

                _logger.LogInformation(
                    "💡 Philips Hue + MQTT → Mezzo {Matricola} | Comando: {Comando} | " +
                    "Stato: {Stato} | Spia: {Colore} | Batteria: {Batteria}%",
                    mezzo.Matricola, TipoComandoIoT.CambiaColoreSpia,
                    mezzo.Stato, coloreSpia, mezzo.LivelloBatteria ?? 0
                );
            }
            catch (Exception hueEx)
            {
                _logger.LogWarning(hueEx, "Impossibile controllare Philips Hue per mezzo {Matricola}", mezzo.Matricola);
            }

            //payload MQTT
            //await _mqttIoTService.PublishAsync($"Parking/{mezzo.IdParcheggioCorrente}/Mezzi/{mezzo.Matricola}", new
            //{
            //    idMezzo = mezzo.Id,
            //    matricola = mezzo.Matricola,
            //    comando = TipoComandoIoT.CambiaColoreSpia.ToString(),
            //    nuovoStato = mezzo.Stato.ToString(),
            //    coloreSpia = coloreSpia.ToString(),
            //    parcheggio = mezzo.ParcheggioCorrente?.Nome,
            //    livelloBatteria = mezzo.LivelloBatteria,
            //    timestamp = DateTime.Now,
            //    source = "API"
            //});

            // NOTA: NON pubblichiamo su MQTT perché il topic Parking/{id}/Mezzi/{matricola}
            // è riservato alla TELEMETRIA dal Gateway. Il MqttIoTBackgroundService si sottoscrive
            // a questo topic per aggiornare il DB basandosi sui messaggi del Gateway.
            // Pubblicare qui causerebbe una doppia scrittura (race condition).
            
            _logger.LogInformation("Stato mezzo {Matricola} aggiornato manualmente da Admin (no MQTT - solo DB)", mezzo.Matricola);

            return Ok(new SuccessResponse
            {
                Messaggio = "Stato del mezzo aggiornato correttamente",
                Dati = new
                {
                    nuovoStato = mezzo.Stato.ToString(),
                    coloreSpia = coloreSpia.ToString(),
                    livelloBatteria = mezzo.LivelloBatteria,
                    parcheggio = mezzo.ParcheggioCorrente?.Nome
                }
            });
        }

        //Mappatura Stato → Colore LED
        private static ColoreSpia GetColoreSpiaPerStato(StatoMezzo stato)
        {
            return stato switch
            {
                StatoMezzo.Disponibile => ColoreSpia.Verde,
                StatoMezzo.InUso => ColoreSpia.Blu,
                StatoMezzo.NonPrelevabile => ColoreSpia.Rosso,
                StatoMezzo.Manutenzione => ColoreSpia.Giallo,
                _ => ColoreSpia.Spenta
            };
        }

        //PUT: api/mezzi/{id}/ricarica
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

            // === PHILIPS HUE: Luce VERDE (mezzo disponibile e ricaricato) ===
            try
            {
                await _philipsHueControl.SetSpiaColor(mezzo.Matricola, ColoreSpia.Verde);

                _logger.LogInformation(
                    "💡 Philips Hue → Mezzo {Matricola} ricaricato | " +
                    "Spia: Rosso → VERDE | Batteria: 100% | Stato: Disponibile",
                    mezzo.Matricola
                );
            }
            catch (Exception hueEx)
            {
                _logger.LogWarning(hueEx, "Impossibile controllare Philips Hue per mezzo {Matricola}", mezzo.Matricola);
            }

            //await _mqttIoTService.PublishAsync($"Parking/{mezzo.IdParcheggioCorrente}/Mezzi/{mezzo.Matricola}", new
            //{
            //    idMezzo = mezzo.Id,
            //    mezzo.Matricola,
            //    comando = TipoComandoIoT.CambiaColoreSpia.ToString(),
            //    coloreSpia = coloreSpia.ToString(),
            //    livelloBatteria = mezzo.LivelloBatteria,
            //    stato = mezzo.Stato.ToString(),
            //    timestamp = DateTime.Now,
            //    source = "API"
            //});

            // NOTA: NON pubblichiamo su MQTT - il topic telemetria è gestito dal Gateway.
            // La ricarica è un'operazione manuale dell'admin che aggiorna solo il DB.
            // Il Gateway sync (ogni 20s) allineerà automaticamente lo stato.

            return Ok(new SuccessResponse
            {
                Messaggio = $"Il mezzo {mezzo.Matricola} è stato ricaricato e reso disponibile.",
                Dati = new
                {
                    mezzo.Id,
                    mezzo.Matricola,
                    mezzo.LivelloBatteria,
                    nuovoStato = mezzo.Stato.ToString(),
                    coloreSpia = ColoreSpia.Verde.ToString()
                }
            });
        }


        //PUT: api/mezzi/matricola/{matricola}/segnala-guasto
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

            // === PHILIPS HUE: Luce ROSSA (mezzo guasto) ===
            try
            {
                await _philipsHueControl.SetSpiaColor(mezzo.Matricola, ColoreSpia.Rosso);

                _logger.LogWarning(
                    "💡 Philips Hue → Mezzo {Matricola} | Comando: {Comando} | " +
                    "Spia: ROSSO 🔴 | Motivo: Guasto segnalato da utente",
                    mezzo.Matricola, TipoComandoIoT.CambiaColoreSpia
                );
            }
            catch (Exception hueEx)
            {
                _logger.LogWarning(hueEx, "Impossibile controllare Philips Hue per mezzo {Matricola}", mezzo.Matricola);
            }

            _logger.LogWarning("Segnalato guasto per mezzo {Matricola}", mezzo.Matricola);

            // Pubblica aggiornamento su MQTT 
            //await _mqttIoTService.PublishAsync($"Parking/{mezzo.IdParcheggioCorrente}/Mezzi/{mezzo.Matricola}", new
            //{
            //    idMezzo = mezzo.Id,
            //    matricola = mezzo.Matricola,
            //    comando = TipoComandoIoT.CambiaColoreSpia.ToString(),
            //    coloreSpia = coloreSpia.ToString(),
            //    stato = mezzo.Stato.ToString(),
            //    motivo = "Guasto segnalato dall’utente",
            //    timestamp = DateTime.Now,
            //    source = "API"
            //});

            // NOTA: NON pubblichiamo su MQTT - il topic telemetria è gestito dal Gateway.
            // La segnalazione guasto è un'operazione che aggiorna solo il DB.
            // Il Gateway sync (ogni 20s) allineerà automaticamente lo stato.

            //Notifica SignalR Admin
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdClaim, out int userId))
                {
                    var utente = await _context.Utenti.FindAsync(userId);

                    //NOTIFICA ALL'ADMIN!
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("SegnalazioneGuasto", new
                        {
                            Matricola = mezzo.Matricola,
                            Tipo = mezzo.Tipo.ToString(),
                            Dettagli = "Segnalato dall’utente"
                        });

                    _logger.LogInformation("Notifica guasto inviata agli admin per mezzo {Matricola} da utente {UserId}", mezzo.Matricola, userId);
                }
                else
                {
                    // Notifica senza info utente
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("NotificaAdmin", new
                        {
                            Titolo = "Mezzo Guasto Segnalato",
                            Testo = $"Il mezzo {mezzo.Matricola} ({mezzo.Tipo}) è stato segnalato come guasto."
                        });
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

            // === PHILIPS HUE: Cambia colore solo se stato è cambiato ===
            if (vecchioStato != mezzo.Stato)
            {
                var coloreSpia = GetColoreSpiaPerStato(mezzo.Stato);

                try
                {
                    await _philipsHueControl.SetSpiaColor(mezzo.Matricola, coloreSpia);

                    _logger.LogWarning(
                        "💡 Philips Hue → Mezzo {Matricola} | Spia: {StatoVecchio} → {Colore} | " +
                        "Batteria: {Batteria}% | Motivo: {Motivo}",
                        mezzo.Matricola, vecchioStato, coloreSpia,
                        mezzo.LivelloBatteria,
                        mezzo.LivelloBatteria < 20 ? "Batteria scarica" : "Stato cambiato"
                    );
                }
                catch (Exception hueEx)
                {
                    _logger.LogWarning(hueEx, "Impossibile controllare Philips Hue per mezzo {Matricola}", mezzo.Matricola);
                }
            }

            //PUBLISH
            //Pubblica solo se c'è un cambiamento significativo
            var diff = Math.Abs((vecchioLivello ?? 0) - (mezzo.LivelloBatteria ?? 0));
            if (diff >= 5 || vecchioStato != mezzo.Stato)
            {
                await _mqttIoTService.PublishAsync("mobishare/mezzo/telemetria", new
                {
                    idMezzo = mezzo.Id,
                    matricola = mezzo.Matricola,
                    comando = vecchioStato != mezzo.Stato ? TipoComandoIoT.CambiaColoreSpia.ToString() : null,
                    coloreSpia = GetColoreSpiaPerStato(mezzo.Stato).ToString(),
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
                        cambiato = vecchioStato != mezzo.Stato,
                        coloreSpia = GetColoreSpiaPerStato(mezzo.Stato).ToString()
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


        // DELETE: api/mezzi/matricola/{matricola} -> elimina mezzo 
        [Authorize(Roles = "Gestore")]
        [HttpDelete("matricola/{matricola}")]
        public async Task<IActionResult> DeleteMezzoByMatricola(string matricola)
        {
            var mezzo = await _context.Mezzi
                .FirstOrDefaultAsync(m => m.Matricola == matricola)
                ?? throw new ElementoNonTrovatoException("Mezzo", matricola);

            //Blocca se il mezzo è in uso
            if (mezzo.Stato == StatoMezzo.InUso)
                throw new OperazioneNonConsentitaException(
                    "Impossibile eliminare un mezzo attualmente in uso");

            //Verifica corse attive (non ancora terminate)
            var haCorseAttive = await _context.Corse
                .AnyAsync(c => c.MatricolaMezzo == matricola && c.DataOraFine == null);

            if (haCorseAttive)
                throw new OperazioneNonConsentitaException(
                    "Impossibile eliminare: il mezzo ha corse non terminate");

            //conta storico corse
            var numeroCorseStoriche = await _context.Corse
                .CountAsync(c => c.MatricolaMezzo == matricola && c.DataOraFine != null);

            if (numeroCorseStoriche > 0)
            {
                //Blocco operazione
                throw new OperazioneNonConsentitaException(
                    $"Impossibile eliminare: il mezzo ha {numeroCorseStoriche} corse nello storico. " +
                    "L'eliminazione comprometterebbe l'integrità dei dati storici.");

                //_logger.LogWarning(
                //    "Eliminazione mezzo {Matricola} con {NumCorseStoriche} corse nello storico",
                //    mezzo.Matricola, numeroCorseStoriche);

            }

            _context.Mezzi.Remove(mezzo);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Mezzo {Matricola} (ID {Id}) eliminato dal sistema",
                mezzo.Matricola, mezzo.Id);

            //Notifica SignalR
            await _hubContext.Clients.Group("admin")
                .SendAsync("NotificaAdmin", "Mezzo Eliminato",
                    $"Il mezzo {mezzo.Matricola} ({mezzo.Tipo}) è stato rimosso dalla flotta");

            return Ok(new SuccessResponse
            {
                Messaggio = $"Mezzo {matricola} eliminato con successo",
                Dati = new
                {
                    id = mezzo.Id,
                    matricola = mezzo.Matricola,
                    tipo = mezzo.Tipo.ToString(),
                    corseStoriche = numeroCorseStoriche
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
