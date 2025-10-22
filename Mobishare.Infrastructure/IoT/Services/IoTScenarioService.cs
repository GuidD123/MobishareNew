using Microsoft.Extensions.Logging;
using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Interfaces;
using Mobishare.IoT.Gateway.Interfaces; 

namespace Mobishare.Infrastructure.IoT.Services
{
    /// <summary>
    /// Implementazione di scenari IoT predefiniti per la demo.
    /// Permette di eseguire sequenze di test automatiche sull'emulatore Gateway
    /// per dimostrazioni e testing.
    /// </summary>
    public class IoTScenarioService : IIoTScenarioService
    {
        private readonly IMqttGatewayEmulatorService _emulatore;
        private readonly ILogger<IoTScenarioService> _logger;

        private bool _isRunning;
        private string? _scenarioCorrente;
        private string? _dettagliScenario;

        public IoTScenarioService(
            IMqttGatewayEmulatorService emulatore,
            ILogger<IoTScenarioService> logger)
        {
            _emulatore = emulatore;
            _logger = logger;
        }

        /// <summary>
        /// Indica se uno scenario è attualmente in esecuzione
        /// </summary>
        public bool IsScenarioInEsecuzione => _isRunning;

        /// <summary>
        /// Nome dello scenario corrente (null se nessuno scenario è attivo)
        /// </summary>
        public string? ScenarioCorrente => _scenarioCorrente;

        /// <summary>
        /// Dettagli testuali dell'ultimo scenario eseguito o in corso
        /// </summary>
        public string? DettagliScenario => _dettagliScenario;

        /// <summary>
        /// Restituisce l'elenco degli scenari predefiniti disponibili
        /// </summary>
        public List<string> GetScenariDisponibili()
        {
            return new List<string>
            {
                "BatteriaScarica",
                "SbloccaMezzo",
                "GuastoMezzo"
            };
        }

        /// <summary>
        /// Avvia uno scenario predefinito su un parcheggio specifico
        /// </summary>
        public async Task AvviaScenarioAsync(string nomeScenario, int idParcheggio)
        {
            if (_isRunning)
            {
                _logger.LogWarning("Uno scenario è già in esecuzione: {Scenario}. Impossibile avviarne un altro.", _scenarioCorrente);
                return;
            }

            _isRunning = true;
            _scenarioCorrente = nomeScenario;
            _dettagliScenario = $"Avviato alle {DateTime.Now:HH:mm:ss} per parcheggio {idParcheggio}";

            _logger.LogInformation("Avvio scenario {Scenario} per parcheggio {IdParcheggio}", nomeScenario, idParcheggio);

            try
            {
                switch (nomeScenario.ToLowerInvariant())
                {
                    case "batteriascarica":
                        await EseguiBatteriaScaricaAsync();
                        break;

                    case "sbloccamezzo":
                        await EseguiSbloccaMezzoAsync();
                        break;

                    case "guastomezzo":
                        await EseguiGuastoMezzoAsync();
                        break;

                    default:
                        _logger.LogWarning("Scenario '{Scenario}' non riconosciuto", nomeScenario);
                        _dettagliScenario += " - ERRORE: scenario sconosciuto";
                        _isRunning = false;
                        _scenarioCorrente = null;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esecuzione dello scenario {Scenario}", nomeScenario);
                _dettagliScenario += $" - ERRORE: {ex.Message}";
                _isRunning = false;
                _scenarioCorrente = null;
            }
        }

        /// <summary>
        /// Ferma lo scenario corrente
        /// </summary>
        public Task FermaScenarioAsync()
        {
            if (!_isRunning)
            {
                _logger.LogInformation("Nessuno scenario in esecuzione da fermare");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Scenario {Scenario} terminato manualmente", _scenarioCorrente);

            _isRunning = false;
            _dettagliScenario += $" - Terminato alle {DateTime.Now:HH:mm:ss}";
            _scenarioCorrente = null;

            return Task.CompletedTask;
        }

        // === SCENARI PRIVATI ===

        private async Task EseguiBatteriaScaricaAsync()
        {
            var mezzi = _emulatore.GetMezziEmulati();
            if (mezzi.Count == 0)
            {
                _dettagliScenario += " - ERRORE: nessun mezzo disponibile nell'emulatore";
                _logger.LogWarning("Scenario BatteriaScarica: nessun mezzo emulato disponibile");
                return;
            }

            var idMezzo = mezzi[0];
            await _emulatore.SimulaVariazioneBatteriaAsync(idMezzo, 10);

            _dettagliScenario += $" - Mezzo {idMezzo} scaricato al 10%";
            _logger.LogInformation("Scenario BatteriaScarica: mezzo {IdMezzo} portato al 10%", idMezzo);
        }

        private async Task EseguiSbloccaMezzoAsync()
        {
            var mezzi = _emulatore.GetMezziEmulati();
            if (mezzi.Count == 0)
            {
                _dettagliScenario += " - ERRORE: nessun mezzo disponibile nell'emulatore";
                _logger.LogWarning("Scenario SbloccaMezzo: nessun mezzo emulato disponibile");
                return;
            }

            var idMezzo = mezzi[0];
            await _emulatore.SimulaCambioStatoAsync(idMezzo, StatoMezzo.InUso);

            _dettagliScenario += $" - Mezzo {idMezzo} sbloccato e portato in uso";
            _logger.LogInformation("Scenario SbloccaMezzo: mezzo {IdMezzo} portato in stato InUso", idMezzo);
        }

        private async Task EseguiGuastoMezzoAsync()
        {
            var mezzi = _emulatore.GetMezziEmulati();
            if (mezzi.Count == 0)
            {
                _dettagliScenario += " - ERRORE: nessun mezzo disponibile nell'emulatore";
                _logger.LogWarning("Scenario GuastoMezzo: nessun mezzo emulato disponibile");
                return;
            }

            var idMezzo = mezzi[0];
            await _emulatore.SimulaCambioStatoAsync(idMezzo, StatoMezzo.Manutenzione);

            _dettagliScenario += $" - Mezzo {idMezzo} forzato in manutenzione per simulare guasto";
            _logger.LogInformation("Scenario GuastoMezzo: mezzo {IdMezzo} portato in Manutenzione", idMezzo);
        }
    }
}