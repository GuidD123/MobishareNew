using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Enums;
using Mobishare.Core.Models;
using Mobishare.IoT.Gateway.Config;
using Mobishare.IoT.Gateway.Interfaces;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Mobishare.IoT.Gateway.Services
{
    public class MqttGatewayEmulatorService : IMqttGatewayEmulatorService, IDisposable
    {
        private readonly ILogger<MqttGatewayEmulatorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IManagedMqttClient _mqttClient;
        private readonly MqttConfiguration _mqttConfig;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ConcurrentDictionary<string, MezzoEmulato> _mezziEmulati;
        private readonly Random _random;

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _batterySimulations = new();

        private bool _disposed = false;
        private bool _isRunning = false;
        private int _idParcheggio;

        public bool IsRunning => _isRunning;
        public int IdParcheggio => _idParcheggio;

        public MqttGatewayEmulatorService(ILogger<MqttGatewayEmulatorService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _mezziEmulati = new ConcurrentDictionary<string, MezzoEmulato>();
            _random = new Random();

            _batterySimulations = new ConcurrentDictionary<string, CancellationTokenSource>();

            _mqttConfig = new MqttConfiguration();
            _configuration.GetSection("Mqtt").Bind(_mqttConfig);
            _mqttConfig.ClientId = "MobishareGatewayEmulator";

            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var factory = new MqttFactory(); 
            _mqttClient = factory.CreateManagedMqttClient();

            _mqttClient.ApplicationMessageReceivedAsync += OnCommandReceivedAsync;
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        }

        public async Task AvviaAsync()
        {
            await StartAsync(1);
        }

        public async Task FermaAsync()
        {
            await StopAsync();
        }

        public async Task StartAsync(int idParcheggio, CancellationToken cancellationToken = default)
        {
            try
            {
                _idParcheggio = idParcheggio;
                _logger.LogInformation("Avvio emulatore Gateway IoT per parcheggio {IdParcheggio}...", idParcheggio);

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId(_mqttConfig.ClientId + "-P" + idParcheggio + "-" + DateTime.Now.Ticks)
                    .WithTcpServer(_mqttConfig.BrokerHost, _mqttConfig.BrokerPort)
                    .WithCleanSession(true)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(_mqttConfig.KeepAliveSeconds));

                if (!string.IsNullOrEmpty(_mqttConfig.Username))
                {
                    clientOptions.WithCredentials(_mqttConfig.Username, _mqttConfig.Password);
                }

                if (_mqttConfig.UseTls)
                {
                    clientOptions.WithTlsOptions(o => o.UseTls());
                }

                var managedOptions = new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions.Build())
                    .WithAutoReconnectDelay(TimeSpan.FromMilliseconds(_mqttConfig.ReconnectDelay))
                    .Build();

                await _mqttClient.StartAsync(managedOptions);
                await SottoscriviAiComandiAsync(idParcheggio);

                _isRunning = true;
                _logger.LogInformation("Emulatore Gateway IoT avviato per parcheggio {IdParcheggio}", idParcheggio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'avvio dell'emulatore Gateway IoT");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Arresto emulatore Gateway IoT...");
                _isRunning = false;
                await _mqttClient.StopAsync();
                _mezziEmulati.Clear();
                _logger.LogInformation("Emulatore Gateway IoT arrestato");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'arresto dell'emulatore Gateway IoT");
                throw;
            }
        }

        private async Task SottoscriviAiComandiAsync(int idParcheggio)
        {
            var commandTopic = $"Parking/{idParcheggio}/Comandi/+";

            var subscription = new[]
            {
                new MqttTopicFilterBuilder()
                    .WithTopic(commandTopic)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build()
            };

            await _mqttClient.SubscribeAsync(subscription);
            _logger.LogInformation("Gateway emulatore sottoscritto ai comandi: {Topic}", commandTopic);
        }


        public async Task AggiungiMezzoEmulato(string idMezzo, string matricola, TipoMezzo tipo, StatoMezzo statoIniziale, int? livelloBatteria)
        {
            var mezzoEmulato = new MezzoEmulato
            {
                IdMezzo = idMezzo,
                Matricola = matricola,
                Tipo = tipo,
                Stato = statoIniziale,
                //Gestione livelloBatteria nullable
                LivelloBatteria = tipo == TipoMezzo.BiciMuscolare
                    ? null
                    : (livelloBatteria ?? 100), // Se null, default 100 per mezzi elettrici
                ColoreSpia = statoIniziale switch
                {
                    StatoMezzo.Disponibile => ColoreSpia.Verde,
                    StatoMezzo.InUso => ColoreSpia.Rosso,
                    StatoMezzo.Manutenzione => ColoreSpia.Giallo,
                    StatoMezzo.NonPrelevabile => ColoreSpia.Rosso,
                    _ => ColoreSpia.Spenta
                },
                UltimoAggiornamento = DateTime.Now
            };

            _mezziEmulati.TryAdd(idMezzo, mezzoEmulato);
            _logger.LogInformation("Mezzo emulato aggiunto: {IdMezzo} ({Tipo}) - Stato: {Stato}, Batteria: {Batteria}%",
                idMezzo, tipo, statoIniziale, tipo == TipoMezzo.BiciMuscolare ? "N/A" : livelloBatteria?.ToString() ?? "100");

            await InviaStatusMezzoAsync(mezzoEmulato);
        }

        public async Task RimuoviMezzoEmulato(string idMezzo)
        {
            if (_mezziEmulati.TryRemove(idMezzo, out _))
            {
                //Ferma il thread di scarica batteria
                if (_batterySimulations.TryRemove(idMezzo, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _logger.LogInformation("Thread batteria fermato per mezzo {IdMezzo}", idMezzo);
                }

                _logger.LogInformation("Mezzo emulato rimosso: {IdMezzo}", idMezzo);
            }
            await Task.CompletedTask;
        }

        public List<string> GetMezziEmulati()
        {
            return _mezziEmulati.Keys.ToList();
        }

        public async Task SimulaCambioStatoAsync(string idMezzo, StatoMezzo nuovoStato)
        {
            if (_mezziEmulati.TryGetValue(idMezzo, out var mezzo))
            {
                mezzo.Stato = nuovoStato;
                mezzo.UltimoAggiornamento = DateTime.Now;

                mezzo.ColoreSpia = nuovoStato switch
                {
                    StatoMezzo.Disponibile => ColoreSpia.Verde,
                    StatoMezzo.InUso => ColoreSpia.Rosso,
                    StatoMezzo.Manutenzione => ColoreSpia.Giallo,
                    StatoMezzo.NonPrelevabile => ColoreSpia.Rosso,
                    _ => ColoreSpia.Spenta
                };

                _logger.LogInformation("Simulato cambio stato mezzo {IdMezzo}: {Stato}", idMezzo, nuovoStato);
                await InviaStatusMezzoAsync(mezzo);
            }
        }

        public async Task SimulaVariazioneBatteriaAsync(string idMezzo, int nuovoLivello)
        {
            if (_mezziEmulati.TryGetValue(idMezzo, out var mezzo))
            {
                mezzo.LivelloBatteria = Math.Clamp(nuovoLivello, 0, 100);
                mezzo.UltimoAggiornamento = DateTime.Now;

                if (mezzo.LivelloBatteria < 20 && mezzo.Tipo != TipoMezzo.BiciMuscolare)
                {
                    mezzo.Stato = StatoMezzo.NonPrelevabile;
                    mezzo.ColoreSpia = ColoreSpia.Rosso;
                }
                else if (mezzo.Stato == StatoMezzo.NonPrelevabile && mezzo.LivelloBatteria >= 20)
                {
                    mezzo.Stato = StatoMezzo.Disponibile;
                    mezzo.ColoreSpia = ColoreSpia.Verde;
                }

                _logger.LogInformation("Simulata variazione batteria mezzo {IdMezzo}: {Livello}%", idMezzo, nuovoLivello);
                await InviaStatusMezzoAsync(mezzo);
            }
        }

        public Task AvviaSimulazioneAutomaticaAsync(TimeSpan intervallo)
        {
            _logger.LogInformation("Avviata simulazione automatica ogni {Intervallo}", intervallo);
            return Task.CompletedTask;
        }

        public Task FermaSimulazioneAutomaticaAsync()
        {
            _logger.LogInformation("Fermata simulazione automatica");
            return Task.CompletedTask;
        }

        private async Task InviaStatusMezzoAsync(MezzoEmulato mezzo)
        {
            try
            {
                var statusMessage = new MezzoStatusMessage
                {
                    IdMezzo = mezzo.IdMezzo,
                    Matricola = mezzo.Matricola,
                    LivelloBatteria = mezzo.Tipo == TipoMezzo.BiciMuscolare ? null : mezzo.LivelloBatteria,
                    Stato = mezzo.Stato,
                    Tipo = mezzo.Tipo,
                    Timestamp = DateTime.Now,
                    Messaggio = $"Status da Gateway - Spia: {mezzo.ColoreSpia}"
                };

                var topic = $"Parking/{_idParcheggio}/Mezzi/{mezzo.IdMezzo}";
                var json = JsonSerializer.Serialize(statusMessage, _jsonOptions);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(json)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

               
                await _mqttClient.EnqueueAsync(message);

                _logger.LogDebug("Status inviato per mezzo {IdMezzo}: {Stato}, Batteria: {Batteria}%",
                    mezzo.IdMezzo, mezzo.Stato, mezzo.LivelloBatteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio dello status del mezzo {IdMezzo}", mezzo.IdMezzo);
            }
        }

        private Task OnCommandReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                _logger.LogDebug("Comando ricevuto su topic {Topic}: {Payload}", topic, payload);

                var topicParts = topic.Split('/');
                if (topicParts.Length >= 4 && topicParts[2] == "Comandi")
                {
                    var idMezzo = topicParts[3];
                    _ = ProcessaComandoAsync(idMezzo, payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella gestione del comando ricevuto");
            }

            return Task.CompletedTask;
        }

        private async Task ProcessaComandoAsync(string idMezzo, string payload)
        {
            try
            {
                //Usa il modello ComandoMezzoMessage
                var comando = JsonSerializer.Deserialize<ComandoMezzoMessage>(payload, _jsonOptions);
                if (comando == null)
                {
                    _logger.LogWarning("Comando deserializzato nullo per mezzo {IdMezzo}", idMezzo);
                    return;
                }

                _logger.LogInformation("Processando comando {Comando} (ID: {CommandId}) per mezzo {IdMezzo}",
                    comando.Comando, comando.CommandId, idMezzo);

                var successo = false;
                var messaggioRisposta = "";

                // Se il mezzo non è presente, potrebbe essere appena arrivato in questo parcheggio
                // Prova a sincronizzarlo dal database prima di fallire
                if (!_mezziEmulati.ContainsKey(idMezzo))
                {
                    _logger.LogInformation("Mezzo {IdMezzo} non trovato nel gateway - tentativo sync on-demand", idMezzo);
                    // Nota: qui non possiamo fare await perché non abbiamo MqttGatewayManager
                    // Il sync on-demand verrà gestito dalla sincronizzazione periodica entro 30s
                    // In alternativa, il comando fallirà e l'utente dovrà riprovare
                }

                if (_mezziEmulati.TryGetValue(idMezzo, out var mezzo))
                {
                    
                    switch (comando.Comando)
                    {
                        case TipoComandoIoT.Sblocca:
                            successo = await ProcessaSbloccoAsync(mezzo);
                            messaggioRisposta = successo ? "Mezzo sbloccato" : "Impossibile sbloccare il mezzo";
                            break;

                        case TipoComandoIoT.Blocca:
                            successo = await ProcessaBloccoAsync(mezzo);
                            messaggioRisposta = successo ? "Mezzo bloccato" : "Impossibile bloccare il mezzo";
                            break;

                        case TipoComandoIoT.CambiaColoreSpia:
                            successo = await ProcessaCambioSpiaAsync(mezzo, comando);
                            messaggioRisposta = successo ? "Colore spia cambiato" : "Impossibile cambiare colore spia";
                            break;

                        case TipoComandoIoT.RichiediBatteria:
                            successo = true;
                            messaggioRisposta = $"Livello batteria: {mezzo.LivelloBatteria}%";
                            break;

                        default:
                            messaggioRisposta = $"Comando {comando.Comando} non supportato";
                            break;
                    }
                }
                else
                {
                    messaggioRisposta = $"Mezzo {idMezzo} non trovato nell'emulazione";
                }

                // Invia risposta con CommandId per correlazione
                await InviaRispostaComandoAsync(idMezzo, comando.CommandId, comando.Comando, successo, messaggioRisposta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel processamento del comando per mezzo {IdMezzo}", idMezzo);
            }
        }

        private async Task<bool> ProcessaSbloccoAsync(MezzoEmulato mezzo)
        {
            //mezzo in db -> disponibile MA MezzoEmulato nell'emulatore non è sincronizzato col database 
            if (mezzo.Stato != StatoMezzo.Disponibile)
                return false;

            mezzo.Stato = StatoMezzo.InUso;
            mezzo.ColoreSpia = ColoreSpia.Rosso;
            mezzo.UltimoAggiornamento = DateTime.Now;

            await InviaStatusMezzoAsync(mezzo);

            // Crea un token per cancellare la simulazione
            var cts = new CancellationTokenSource();
            _batterySimulations.TryAdd(mezzo.IdMezzo, cts);

            // Avvia thread simulazione scarica batteria
            if (mezzo.Tipo != TipoMezzo.BiciMuscolare && mezzo.LivelloBatteria.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    // Usa una variabile locale per evitare problemi con nullable
                    int batteria = mezzo.LivelloBatteria.Value;

                try{
                        while (mezzo.Stato == StatoMezzo.InUso && batteria > 0 && !cts.Token.IsCancellationRequested)
                        {
                            // Simula consumo realistico per demo
                            var consumo = mezzo.Tipo switch
                            {
                                TipoMezzo.MonopattinoElettrico => _random.Next(1, 3), // 1–2% ogni 10s (~10% al minuto)
                                TipoMezzo.BiciElettrica => 1,                         // 1% ogni 10s (~6% al minuto)
                                _ => 0
                            };

                            batteria = Math.Max(0, batteria - consumo);
                            mezzo.LivelloBatteria = batteria;

                            // Se scende sotto 20%, segna come non prelevabile
                            if (batteria < 20)
                            {
                                mezzo.Stato = StatoMezzo.NonPrelevabile;
                                mezzo.ColoreSpia = ColoreSpia.Rosso;

                                _logger.LogWarning("Mezzo {Matricola} con batteria critica: {Batteria}%",
                                    mezzo.Matricola, batteria);
                            }

                            // Pubblica aggiornamento telemetria
                            await InviaStatusMezzoAsync(mezzo);

                            // Attendi 10 secondi prima del prossimo aggiornamento
                            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Simulazione batteria terminata per mezzo {Matricola}", mezzo.Matricola);
                    }
                    finally
                    {
                        _batterySimulations.TryRemove(mezzo.IdMezzo, out _);
                    }
                }, cts.Token);
            }

            return true;
        }

        private async Task<bool> ProcessaBloccoAsync(MezzoEmulato mezzo)
        {
            if (mezzo.Stato != StatoMezzo.InUso)
                return false;

            if (_batterySimulations.TryRemove(mezzo.IdMezzo, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            mezzo.Stato = StatoMezzo.Disponibile;
            mezzo.ColoreSpia = ColoreSpia.Verde;
            mezzo.UltimoAggiornamento = DateTime.Now;

            await InviaStatusMezzoAsync(mezzo);
            _logger.LogInformation("Mezzo {Matricola} bloccato e simulazione batteria fermata", mezzo.Matricola);
            return true;
        }

        private async Task<bool> ProcessaCambioSpiaAsync(MezzoEmulato mezzo, ComandoMezzoMessage comando)
        {
            if (comando.Parametri?.ContainsKey("Colore") == true)
            {
                if (Enum.TryParse<ColoreSpia>(comando.Parametri["Colore"].ToString(), out var nuovoColore))
                {
                    mezzo.ColoreSpia = nuovoColore;
                    mezzo.UltimoAggiornamento = DateTime.Now;
                    await InviaStatusMezzoAsync(mezzo);
                    return true;
                }
            }
            return false;
        }

        private async Task InviaRispostaComandoAsync(string idMezzo, string commandId, TipoComandoIoT comandoOriginale, bool successo, string messaggio)
        {
            try
            {
                var risposta = new RispostaComandoMessage
                {
                    IdMezzo = idMezzo,
                    CommandId = commandId,  
                    ComandoOriginale = comandoOriginale,
                    Successo = successo,
                    ErroreDescrizione = successo ? null : messaggio,
                    Timestamp = DateTime.Now,
                    DatiAggiuntivi = successo ? new Dictionary<string, object> { { "Messaggio", messaggio } } : null
                };

                var topic = $"Parking/{_idParcheggio}/RisposteComandi/{idMezzo}";
                var json = JsonSerializer.Serialize(risposta, _jsonOptions);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(json)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.EnqueueAsync(message);

                _logger.LogDebug("Risposta inviata per comando {Comando} su mezzo {IdMezzo}: {Successo}",
                    comandoOriginale, idMezzo, successo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio della risposta comando");
            }
        }

        private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation("Gateway emulatore connesso al broker MQTT");
            return Task.CompletedTask;
        }

        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogWarning("Gateway emulatore disconnesso dal broker MQTT: {Reason}", e.Reason);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _mqttClient?.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}