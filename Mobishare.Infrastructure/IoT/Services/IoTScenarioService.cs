using Microsoft.Extensions.Logging;
using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Interfaces;
using Mobishare.IoT.Gateway.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobishare.Infrastructure.IoT.Services
{
    /// <summary>
    /// Implementazione di scenari IoT predefiniti per la demo
    /// </summary>
    public class IoTScenarioService : IIoTScenarioService
    {
        private readonly IMqttGatewayEmulatorService _emulatore;
        private readonly ILogger<IoTScenarioService> _logger;

        private bool _isRunning;
        private string? _scenarioCorrente;
        private string? _dettagliScenario;

        public IoTScenarioService(IMqttGatewayEmulatorService emulatore,
                                  ILogger<IoTScenarioService> logger)
        {
            _emulatore = emulatore;
            _logger = logger;
        }

        public bool IsScenarioInEsecuzione => _isRunning;
        public string? ScenarioCorrente => _scenarioCorrente;
        public string? DettagliScenario => _dettagliScenario;

        public List<string> GetScenariDisponibili()
        {
            return
            [
                "BatteriaScarica",
                "SbloccaMezzo",
                "GuastoMezzo"
            ];
        }

        public async Task AvviaScenarioAsync(string nomeScenario, int idParcheggio)
        {
            if (_isRunning)
            {
                _logger.LogWarning("Uno scenario è già in esecuzione: {Scenario}", _scenarioCorrente);
                return;
            }

            _isRunning = true;
            _scenarioCorrente = nomeScenario;
            _dettagliScenario = $"Avviato alle {DateTime.UtcNow} per parcheggio {idParcheggio}";

            switch (nomeScenario.ToLower())
            {
                case "batteriascarica":
                    var mezzi1 = _emulatore.GetMezziEmulati();
                    if (mezzi1.Count > 0)
                    {
                        await _emulatore.SimulaVariazioneBatteriaAsync(mezzi1[0], 10);
                        _dettagliScenario += " - Mezzo scaricato al 10%";
                    }
                    break;

                case "sbloccamezzo":
                    var mezzi2 = _emulatore.GetMezziEmulati();
                    if (mezzi2.Count > 0)
                    {
                        await _emulatore.SimulaCambioStatoAsync(mezzi2[0], StatoMezzo.InUso);
                        _dettagliScenario += " - Mezzo sbloccato e in uso";
                    }
                    break;

                case "guastomezzo":
                    var mezzi3 = _emulatore.GetMezziEmulati();
                    if (mezzi3.Count > 0)
                    {
                        await _emulatore.SimulaCambioStatoAsync(mezzi3[0], StatoMezzo.Manutenzione);
                        _dettagliScenario += " - Mezzo forzato in manutenzione";
                    }
                    break;

                default:
                    _logger.LogWarning("Scenario {Scenario} non riconosciuto", nomeScenario);
                    _isRunning = false;
                    _scenarioCorrente = null;
                    _dettagliScenario = null;
                    break;
            }

            _logger.LogInformation("Scenario {Scenario} avviato", nomeScenario);
        }

        public Task FermaScenarioAsync()
        {
            if (!_isRunning) return Task.CompletedTask;

            _logger.LogInformation("Scenario {Scenario} terminato", _scenarioCorrente);

            _isRunning = false;
            _scenarioCorrente = null;
            _dettagliScenario = $"Terminato alle {DateTime.UtcNow}";

            return Task.CompletedTask;
        }
    }
}


//Così ho le proprietà gestite : IsScenarioInEsecuzione, ScenarioCorrente, DettagliScenario
//Ho 3 scenari minimi: BatteriaScarica, SbloccaMezzo, GuastoMezzo 
//Il metodo GetScenarioDisponibili() mi ritorna l'elenco degli scenari 