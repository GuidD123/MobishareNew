/*using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mobishare.API.DTO;
using Mobishare.Infrastructure.IoT.Interfaces;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/iot/emulator")]
    [Authorize("AdminOnly")]
    public class IoTEmulatorController(IMqttGatewayEmulatorService emulatorService) : ControllerBase
    {
        private readonly IMqttGatewayEmulatorService _emulatorService = emulatorService;

        /// <summary>
        /// Avvia l'emulatore Gateway IoT per un parcheggio specifico
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartEmulator([FromBody] AvvioEmulatoreRequest request)
        {
            try
            {
                if (_emulatorService.IsRunning)
                {
                    return BadRequest(new MqttOperationResponse
                    {
                        Success = false,
                        Message = $"Emulatore già in esecuzione per parcheggio {_emulatorService.IdParcheggio}"
                    });
                }

                await _emulatorService.StartAsync(request.IdParcheggio);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Emulatore Gateway IoT avviato per parcheggio {request.IdParcheggio}"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new MqttOperationResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Ferma l'emulatore Gateway IoT
        /// </summary>
        [HttpPost("stop")]
        public async Task<IActionResult> StopEmulator()
        {
            try
            {
                if (!_emulatorService.IsRunning)
                {
                    return BadRequest(new MqttOperationResponse
                    {
                        Success = false,
                        Message = "Emulatore non è in esecuzione"
                    });
                }

                await _emulatorService.StopAsync();

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = "Emulatore Gateway IoT arrestato"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new MqttOperationResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Aggiunge un mezzo all'emulazione
        /// </summary>
        [HttpPost("mezzi/add")]
        public async Task<IActionResult> AddMezzoEmulato([FromBody] AggiungiMezzoEmulatorRequest request)
        {
            try
            {
                if (!_emulatorService.IsRunning)
                {
                    return BadRequest(new MqttOperationResponse
                    {
                        Success = false,
                        Message = "Emulatore non è in esecuzione. Avviarlo prima di aggiungere mezzi."
                    });
                }

                await _emulatorService.AggiungiMezzoEmulato(request.IdMezzo, request.Matricola, request.Tipo);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Mezzo {request.IdMezzo} ({request.Tipo}) aggiunto all'emulazione"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new MqttOperationResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Rimuove un mezzo dall'emulazione
        /// </summary>
        [HttpDelete("mezzi/{idMezzo}")]
        public async Task<IActionResult> RemoveMezzoEmulato(string idMezzo)
        {
            try
            {
                await _emulatorService.RimuoviMezzoEmulato(idMezzo);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Mezzo {idMezzo} rimosso dall'emulazione"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new MqttOperationResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Simula un cambio di stato per un mezzo emulato
        /// </summary>
        [HttpPost("simula/stato")]
        public async Task<IActionResult> SimulaCambioStato([FromBody] SimulaCambioStatoRequest request)
        {
            try
            {
                await _emulatorService.SimulaCambioStatoAsync(request.IdMezzo, request.NuovoStato);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Simulato cambio stato per mezzo {request.IdMezzo}: {request.NuovoStato}"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new MqttOperationResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Simula una variazione di batteria per un mezzo emulato
        /// </summary>
        [HttpPost("simula/batteria")]
        public async Task<IActionResult> SimulaVariazioneBatteria([FromBody] SimulaVariazioneBatteriaRequest request)
        {
            try
            {
                await _emulatorService.SimulaVariazioneBatteriaAsync(request.IdMezzo, request.NuovoLivello);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Simulata variazione batteria per mezzo {request.IdMezzo}: {request.NuovoLivello}%"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new MqttOperationResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Avvia la simulazione automatica dell'emulatore
        /// </summary>
        [HttpPost("auto/start")]
        public async Task<IActionResult> AvviaSimulazioneAutomatica([FromBody] AvviaSimulazioneAutomaticaRequest request)
        {
            try
            {
                var intervallo = TimeSpan.FromSeconds(request.IntervalloSecondi);
                await _emulatorService.AvviaSimulazioneAutomaticaAsync(intervallo);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Simulazione automatica avviata (ogni {request.IntervalloSecondi} secondi)"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new MqttOperationResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Ferma la simulazione automatica dell'emulatore
        /// </summary>
        [HttpPost("auto/stop")]
        public async Task<IActionResult> FermaSimulazioneAutomatica()
        {
            try
            {
                await _emulatorService.FermaSimulazioneAutomaticaAsync();

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = "Simulazione automatica fermata"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new MqttOperationResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Ottiene lo status corrente dell'emulatore
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous] // Status può essere pubblico
        public IActionResult GetEmulatorStatus()
        {
            return Ok(new EmulatorStatusResponse
            {
                IsRunning = _emulatorService.IsRunning,
                IdParcheggio = _emulatorService.IsRunning ? _emulatorService.IdParcheggio : -1,
                MezziEmulati = _emulatorService.GetMezziEmulati()
            });
        }
    }
}*/