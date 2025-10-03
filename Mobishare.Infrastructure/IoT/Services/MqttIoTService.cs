using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using Mobishare.Infrastructure.IoT.Models;
using Mobishare.Infrastructure.IoT.Config;
using Mobishare.Infrastructure.IoT.Events;
using Mobishare.Infrastructure.IoT.Interfaces;
using Mobishare.Core.Enums;


namespace Mobishare.Infrastructure.IoT.Services
{
    /// <summary>
    /// Servizio MQTT per la comunicazione del Backend con il sottosistema IoT
    /// </summary>
    public class MqttIoTService : IMqttIoTService, IDisposable
    {
        private readonly ILogger<MqttIoTService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IManagedMqttClient _mqttClient;
        private readonly MqttConfiguration _mqttConfig;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;

        // Eventi pubblici
        public event EventHandler<MezzoStatusReceivedEventArgs>? MezzoStatusReceived;
        public event EventHandler<RispostaComandoReceivedEventArgs>? RispostaComandoReceived;

        public bool IsConnected => _mqttClient.IsConnected;

        public MqttIoTService(ILogger<MqttIoTService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Carica configurazione MQTT
            _mqttConfig = new MqttConfiguration();
            _configuration.GetSection("Mqtt").Bind(_mqttConfig);

            // JsonSerializerOptions riutilizzabile
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Crea client MQTT gestito
            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();

            // Configura eventi del client MQTT
            _mqttClient.ApplicationMessageReceivedAsync += OnMqttMessageReceivedAsync;
            _mqttClient.ConnectedAsync += OnMqttConnectedAsync;
            _mqttClient.DisconnectedAsync += OnMqttDisconnectedAsync;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Avvio servizio MQTT IoT...");

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId(_mqttConfig.ClientId + "-" + Environment.MachineName + "-" + DateTime.Now.Ticks)
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

                // Sottoscrivi ai topic di interesse
                await SottoscriviTopicsAsync();

                _logger.LogInformation("Servizio MQTT IoT avviato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'avvio del servizio MQTT IoT");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Arresto servizio MQTT IoT...");
                await _mqttClient.StopAsync();
                _logger.LogInformation("Servizio MQTT IoT arrestato");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'arresto del servizio MQTT IoT");
                throw;
            }
        }

        private async Task SottoscriviTopicsAsync()
        {
            var subscriptions = new[]
            {
                // Sottoscrivi a tutti i status dei mezzi
                new MqttTopicFilterBuilder()
                    .WithTopic(MqttTopics.TuttiMezzi)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build(),
                
                // Sottoscrivi a tutte le risposte ai comandi
                new MqttTopicFilterBuilder()
                    .WithTopic(MqttTopics.TutteRisposte)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build()
            };

            await _mqttClient.SubscribeAsync(subscriptions);

            _logger.LogInformation("Sottoscrizioni MQTT completate: {Topics}",
                string.Join(", ", subscriptions.Select(s => s.Topic)));
        }

        // === METODI PUBBLICI PER INVIO COMANDI ===

        public async Task InviaComandoMezzoAsync(int idParcheggio, string idMezzo, ComandoMezzoMessage comando)
        {
            try
            {
                var topic = MqttTopics.GetComandoMezzoTopic(idParcheggio, idMezzo);
                await PublishAsync(topic, comando);

                _logger.LogInformation("Comando {Comando} inviato al mezzo {IdMezzo} nel parcheggio {IdParcheggio}",
                    comando.Comando, idMezzo, idParcheggio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'invio del comando {Comando} al mezzo {IdMezzo}",
                    comando.Comando, idMezzo);
                throw;
            }
        }

        public async Task SbloccaMezzoAsync(int idParcheggio, string idMezzo, string utenteId)
        {
            var comando = new ComandoMezzoMessage
            {
                IdMezzo = idMezzo,
                Comando = TipoComandoIoT.Sblocca,
                MittenteId = utenteId,
                Parametri = new Dictionary<string, object>
                {
                    { "UtenteId", utenteId },
                    { "Timestamp", DateTime.UtcNow }
                }
            };

            await InviaComandoMezzoAsync(idParcheggio, idMezzo, comando);
        }

        public async Task BloccaMezzoAsync(int idParcheggio, string idMezzo)
        {
            var comando = new ComandoMezzoMessage
            {
                IdMezzo = idMezzo,
                Comando = TipoComandoIoT.Blocca,
                MittenteId = "System"
            };

            await InviaComandoMezzoAsync(idParcheggio, idMezzo, comando);
        }

        public async Task CambiaColoreSpiaAsync(int idParcheggio, string idMezzo, ColoreSpia colore)
        {
            var comando = new ComandoMezzoMessage
            {
                IdMezzo = idMezzo,
                Comando = TipoComandoIoT.CambiaColoreSpia,
                MittenteId = "System",
                Parametri = new Dictionary<string, object>
                {
                    { "Colore", colore.ToString() }
                }
            };

            await InviaComandoMezzoAsync(idParcheggio, idMezzo, comando);
        }

        public async Task PublishAsync<T>(string topic, T message) where T : class
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);

            await SafePublishAsync(async () =>
            {
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(json)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.EnqueueAsync(mqttMessage);
            });
        }


        /*public async Task PublishAsync(string topic, string jsonMessage)
        {
            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonMessage)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.EnqueueAsync(message);

                _logger.LogDebug("Messaggio pubblicato sul topic {Topic}: {Message}", topic, jsonMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella pubblicazione del messaggio sul topic {Topic}", topic);
                throw;
            }
        }*/

        public async Task PublishAsync(string topic, string jsonMessage)
        {
            await SafePublishAsync(async () =>
            {
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonMessage)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.EnqueueAsync(mqttMessage);
            });
        }

        // === EVENT HANDLERS INTERNI ===

        private async Task OnMqttMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                _logger.LogDebug("Messaggio ricevuto dal topic {Topic}: {Payload}", topic, payload);

                // Parse del topic per determinare il tipo di messaggio
                await RouteMessageAsync(topic, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella gestione del messaggio MQTT ricevuto");
            }
        }

        private async Task RouteMessageAsync(string topic, string payload)
        {
            try
            {
                // Parse del topic: Parking/{id_parking}/{messageType}/...
                var topicParts = topic.Split('/');
                if (topicParts.Length < 3 || topicParts[0] != "Parking")
                {
                    _logger.LogWarning("Topic non riconosciuto: {Topic}", topic);
                    return;
                }

                if (!int.TryParse(topicParts[1], out var idParcheggio))
                {
                    _logger.LogWarning("ID parcheggio non valido nel topic: {Topic}", topic);
                    return;
                }

                var messageType = topicParts[2];

                switch (messageType)
                {
                    case "Mezzi":
                        await HandleMezzoStatusMessageAsync(idParcheggio, payload, topic);
                        break;

                    case "RisposteComandi":
                        if (topicParts.Length >= 4)
                        {
                            var idMezzo = topicParts[3];
                            await HandleRispostaComandoMessageAsync(idParcheggio, idMezzo, payload, topic);
                        }
                        break;

                    default:
                        _logger.LogDebug("Tipo di messaggio non gestito: {MessageType} per topic {Topic}",
                            messageType, topic);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel routing del messaggio per topic {Topic}", topic);
            }
        }

        private async Task HandleMezzoStatusMessageAsync(int idParcheggio, string payload, string topic)
        {
            try
            {
                var statusMessage = JsonSerializer.Deserialize<MezzoStatusMessage>(payload,_jsonOptions);

                if (statusMessage != null)
                {
                    _logger.LogInformation("Status ricevuto per mezzo {IdMezzo}: {Stato}, Batteria: {Batteria}%",
                        statusMessage.IdMezzo, statusMessage.Stato, statusMessage.LivelloBatteria);

                    // Scatena l'evento
                    var eventArgs = new MezzoStatusReceivedEventArgs
                    {
                        IdParcheggio = idParcheggio,
                        StatusMessage = statusMessage,
                        Topic = topic,
                        ReceivedAt = DateTime.UtcNow
                    };

                    MezzoStatusReceived?.Invoke(this, eventArgs);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella deserializzazione del messaggio status mezzo");
            }
        }

        private async Task HandleRispostaComandoMessageAsync(int idParcheggio, string idMezzo, string payload, string topic)
        {
            try
            {
                var rispostaMessage = JsonSerializer.Deserialize<RispostaComandoMessage>(payload, _jsonOptions);

                if (rispostaMessage != null)
                {
                    _logger.LogInformation("Risposta comando ricevuta da mezzo {IdMezzo}: {Comando} -> {Successo}",
                        idMezzo, rispostaMessage.ComandoOriginale, rispostaMessage.Successo);

                    // Scatena l'evento
                    var eventArgs = new RispostaComandoReceivedEventArgs
                    {
                        IdParcheggio = idParcheggio,
                        IdMezzo = idMezzo,
                        RispostaMessage = rispostaMessage,
                        Topic = topic,
                        ReceivedAt = DateTime.UtcNow
                    };

                    RispostaComandoReceived?.Invoke(this, eventArgs);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella deserializzazione della risposta comando");
            }
        }

        private async Task OnMqttConnectedAsync(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation("Client MQTT connesso al broker {Host}:{Port}",
                _mqttConfig.BrokerHost, _mqttConfig.BrokerPort);
            await Task.CompletedTask;
        }

        private async Task OnMqttDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogWarning("Client MQTT disconnesso dal broker. Motivo: {Reason}",
                e.Reason);
            await Task.CompletedTask;
        }

        // === DISPOSE ===

        public void Dispose()
        {
            if (!_disposed)
            {
                _mqttClient?.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        private async Task SafePublishAsync(Func<Task> publishAction, int maxRetries = 3, int delayMs = 1000)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await publishAction();
                    return; // pubblicazione riuscita
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "MQTT publish fallito dopo {MaxRetries} tentativi", maxRetries);
                        throw;
                    }

                    _logger.LogWarning(ex, "Tentativo {Attempt}/{MaxRetries} fallito. Riprovo tra {DelayMs}ms", attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs);
                }
            }
        }


    }
}