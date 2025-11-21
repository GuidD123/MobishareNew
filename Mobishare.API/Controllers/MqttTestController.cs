/*using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mobishare.API.DTO;
using Mobishare.Infrastructure.IoT.Interfaces;

namespace Mobishare.API.Controllers
{
    [ApiController]
    [Route("api/mqtt")]
    [Authorize("AdminOnly")]
    public class MqttTestController(IMqttIoTService mqttService) : ControllerBase
    {
        private readonly IMqttIoTService _mqttService = mqttService;

        /// <summary>
        /// Testa il comando di sblocco di un mezzo via MQTT
        /// </summary>
        [HttpPost("test/sblocca")]
        public async Task<IActionResult> SbloccaMezzo([FromBody] SbloccaMezzoRequest request)
        {
            try
            {
                await _mqttService.SbloccaMezzoAsync(request.IdParcheggio, request.IdMezzo, request.UtenteId);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Comando sblocca inviato al mezzo {request.IdMezzo} nel parcheggio {request.IdParcheggio}"
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
        /// Testa il comando di blocco di un mezzo via MQTT
        /// </summary>
        [HttpPost("test/blocca")]
        public async Task<IActionResult> BloccaMezzo([FromBody] BloccaMezzoRequest request)
        {
            try
            {
                await _mqttService.BloccaMezzoAsync(request.IdParcheggio, request.IdMezzo);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Comando blocca inviato al mezzo {request.IdMezzo} nel parcheggio {request.IdParcheggio}"
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
        /// Testa il comando di cambio colore spia via MQTT
        /// </summary>
        [HttpPost("test/spia")]
        public async Task<IActionResult> CambiaColoreSpia([FromBody] CambiaColoreSpiaRequest request)
        {
            try
            {
                await _mqttService.CambiaColoreSpiaAsync(request.IdParcheggio, request.IdMezzo, request.Colore);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Comando cambio spia ({request.Colore}) inviato al mezzo {request.IdMezzo}"
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
        /// Invia un comando generico a un mezzo via MQTT
        /// </summary>
        [HttpPost("test/comando")]
        public async Task<IActionResult> InviaComando([FromBody] InviaComandoRequest request)
        {
            try
            {
                var comando = new Mobishare.Core.Models.ComandoMezzoMessage
                {
                    IdMezzo = request.IdMezzo,
                    Comando = request.Comando,
                    MittenteId = request.UtenteId,
                    Parametri = request.Parametri
                };

                await _mqttService.InviaComandoMezzoAsync(request.IdParcheggio, request.IdMezzo, comando);

                return Ok(new MqttOperationResponse
                {
                    Success = true,
                    Message = $"Comando {request.Comando} inviato al mezzo {request.IdMezzo}"
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
        /// Verifica lo stato della connessione MQTT
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous] // Status può essere pubblico
        public IActionResult GetMqttStatus()
        {
            return Ok(new MqttStatusResponse
            {
                Connected = _mqttService.IsConnected,
                BrokerInfo = "localhost:1883"
            });
        }
    }
}*/