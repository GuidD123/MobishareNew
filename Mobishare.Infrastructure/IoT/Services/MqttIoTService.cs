using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Enums;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.IoT.Events;
using Mobishare.Infrastructure.IoT.Events.Config;
using Mobishare.Infrastructure.IoT.Interfaces;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Mobishare.Infrastructure.IoT.Services
{
    /// <summary>
    /// Servizio MQTT per la comunicazione Backend ↔ IoT.
    ///
    /// Stato:
    /// - Usa IManagedMqttClient (enqueue + retry automatico, QoS1 consigliato).
    /// - Si avvia come IHostedService (registrazione DI: Singleton + HostedService).
    /// - Esegue resubscribe ad ogni reconnect e pubblica stato online/offline con retained message.
    /// - I metodi di Publish NON rilanciano eccezioni (i controller hanno già commitato DB).
    /// </summary>
    public class MqttIoTService : IMqttIoTService, IHostedService, IDisposable
    {
        private readonly ILogger<MqttIoTService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IManagedMqttClient _mqttClient;
        private readonly MqttConfiguration _mqttConfig;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;

        // === Eventi pubblici esposti al resto dell'app ===
        public event EventHandler<MezzoStatusReceivedEventArgs>? MezzoStatusReceived;
        public event EventHandler<RispostaComandoReceivedEventArgs>? RispostaComandoReceived;

        /// <summary>
        /// Indica lo stato di connessione del client gestito.
        /// </summary>
        public bool IsConnected => _mqttClient.IsConnected;

        /// <summary>
        /// Ctor: legge configurazione, crea ManagedMqttClient e registra gli handler degli eventi MQTT.
        /// </summary>
        public MqttIoTService(ILogger<MqttIoTService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Carica configurazione MQTT da appsettings (sezione "Mqtt").
            _mqttConfig = new MqttConfiguration();
            _configuration.GetSection("Mqtt").Bind(_mqttConfig);

            // Opzioni JSON riutilizzabili per serializzazione payload.
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Crea client MQTT gestito (con coda interna + auto-reconnect).
            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();

            // Registra gli handler: ricezione messaggi, connect/disconnect.
            _mqttClient.ApplicationMessageReceivedAsync += OnMqttMessageReceivedAsync;
            _mqttClient.ConnectedAsync += OnMqttConnectedAsync;
            _mqttClient.DisconnectedAsync += OnMqttDisconnectedAsync;
        }

        // =====================
        // IHostedService API
        // =====================

        /// <summary>
        /// Avvio del servizio come HostedService.
        /// </summary>
        public Task StartAsync(CancellationToken ct) => StartInternalAsync(ct);

        /// <summary>
        /// Arresto del servizio come HostedService.
        /// </summary>
        public Task StopAsync(CancellationToken ct) => StopInternalAsync(ct);

        /// <summary>
        /// Avvio effettivo del client MQTT gestito.
        /// - Configura opzioni client (KeepAlive, credenziali, TLS opzionale).
        /// - Imposta Last-Will "offline" retained su topic di stato backend.
        /// - Usa CleanSession(true) per evitare accumulo messaggi non consegnati.
        /// </summary>
        private async Task StartInternalAsync(CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Avvio servizio MQTT IoT...");

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId($"{_mqttConfig.ClientId}-{Environment.MachineName}-{DateTime.Now.Ticks}")
                    .WithTcpServer(_mqttConfig.BrokerHost, _mqttConfig.BrokerPort)
                    .WithCleanSession(true)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(_mqttConfig.KeepAliveSeconds))
                    .WithWillTopic("mobishare/backend/status")
                    .WithWillPayload("{\"status\":\"offline\"}")
                    .WithWillRetain(true)
                    .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

                if (!string.IsNullOrEmpty(_mqttConfig.Username))
                    clientOptions.WithCredentials(_mqttConfig.Username, _mqttConfig.Password);

                if (_mqttConfig.UseTls)
                    clientOptions.WithTlsOptions(o => o.UseTls());

                var managedOptions = new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions.Build())
                    .WithAutoReconnectDelay(TimeSpan.FromMilliseconds(_mqttConfig.ReconnectDelay))
                    .Build();

                await _mqttClient.StartAsync(managedOptions);

                _logger.LogInformation("Servizio MQTT IoT avviato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'avvio del servizio MQTT IoT");
                throw;
            }
        }

        /// <summary>
        /// Arresto ordinato del Managed client.
        /// </summary>
        private async Task StopInternalAsync(CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Arresto servizio MQTT IoT...");

                // Pubblica offline prima di stoppare
                var offline = new MqttApplicationMessageBuilder()
                    .WithTopic("mobishare/backend/status")
                    .WithPayload("{\"status\":\"offline\"}")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(true)
                    .Build();
                await _mqttClient.EnqueueAsync(offline);
                await Task.Delay(300, ct); // Breve attesa

                await _mqttClient.StopAsync();
                _logger.LogInformation("Servizio MQTT IoT arrestato");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'arresto del servizio MQTT IoT");
                throw;
            }
        }

        /// <summary>
        /// Sottoscrive i topic necessari. Chiamata ad ogni reconnect.
        /// </summary>
        private async Task SottoscriviTopicsAsync()
        {
            var subscriptions = new[]
            {
                new MqttTopicFilterBuilder()
                    .WithTopic(MqttTopics.TuttiMezzi)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build(),

                new MqttTopicFilterBuilder()
                    .WithTopic(MqttTopics.TutteRisposte)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build()
            };

            // Previene crash su reconnect
            try
            {
                await _mqttClient.SubscribeAsync(subscriptions);
                _logger.LogInformation("Sottoscrizioni MQTT completate: {Topics}",
                    string.Join(", ", subscriptions.Select(s => s.Topic)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscribe MQTT fallita");
            }
        }

        // =====================
        // COMANDI PUBBLICI
        // =====================

        /// <summary>
        /// Costruisce il topic corretto e pubblica il comando verso un mezzo.
        /// </summary>
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

        /// <summary>
        /// Invia comando di sblocco mezzo con CommandId per idempotenza.
        /// </summary>
        public async Task SbloccaMezzoAsync(int idParcheggio, string idMezzo, string utenteId)
        {
            var comando = new ComandoMezzoMessage
            {
                CommandId = Guid.NewGuid().ToString(),
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

        /// <summary>
        /// Invia comando di blocco mezzo con CommandId per idempotenza.
        /// </summary>
        public async Task BloccaMezzoAsync(int idParcheggio, string idMezzo)
        {
            var comando = new ComandoMezzoMessage
            {
                CommandId = Guid.NewGuid().ToString(),
                IdMezzo = idMezzo,
                Comando = TipoComandoIoT.Blocca,
                MittenteId = "System"
            };
            await InviaComandoMezzoAsync(idParcheggio, idMezzo, comando);
        }

        /// <summary>
        /// Cambia il colore della spia su un mezzo con CommandId per idempotenza.
        /// </summary>
        public async Task CambiaColoreSpiaAsync(int idParcheggio, string idMezzo, ColoreSpia colore)
        {
            var comando = new ComandoMezzoMessage
            {
                CommandId = Guid.NewGuid().ToString(),
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

        // =====================
        // PUBLISH HELPERS
        // =====================

        /// <summary>
        /// Serializza l'oggetto in JSON e lo pubblica sul topic indicato con QoS1.
        /// Non rilancia eccezioni di enqueue: log warning e prosegue.
        /// </summary>
        public async Task PublishAsync<T>(string topic, T message) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(message, _jsonOptions);
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(json)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.EnqueueAsync(mqttMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enqueue MQTT fallito per topic {Topic}", topic);
            }
        }

        /// <summary>
        /// Variante che accetta JSON già pronto.
        /// </summary>
        public async Task PublishAsync(string topic, string jsonMessage)
        {
            try
            {
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonMessage)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.EnqueueAsync(mqttMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enqueue MQTT fallito per topic {Topic}", topic);
            }
        }

        // =====================
        // EVENT HANDLERS MQTT
        // =====================

        /// <summary>
        /// Handler globale di ricezione messaggi.
        /// </summary>
        private async Task OnMqttMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                _logger.LogDebug("Messaggio ricevuto dal topic {Topic}: {Payload}", topic, payload);

                await RouteMessageAsync(topic, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella gestione del messaggio MQTT ricevuto");
            }
        }

        /// <summary>
        /// Instrada il messaggio in base al topic.
        /// </summary>
        private async Task RouteMessageAsync(string topic, string payload)
        {
            try
            {
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

        /// <summary>
        /// Deserializza e propaga un messaggio di stato mezzo.
        /// </summary>
        private async Task HandleMezzoStatusMessageAsync(int idParcheggio, string payload, string topic)
        {
            try
            {
                var statusMessage = JsonSerializer.Deserialize<MezzoStatusMessage>(payload, _jsonOptions);

                if (statusMessage != null)
                {
                    _logger.LogInformation("Status ricevuto per mezzo {IdMezzo}: {Stato}, Batteria: {Batteria}%",
                        statusMessage.IdMezzo, statusMessage.Stato, statusMessage.LivelloBatteria);

                    var eventArgs = new MezzoStatusReceivedEventArgs
                    {
                        IdParcheggio = idParcheggio,
                        StatusMessage = statusMessage,
                        Topic = topic,
                        ReceivedAt = DateTime.UtcNow
                    };

                    try
                    {
                        MezzoStatusReceived?.Invoke(this, eventArgs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore in handler MezzoStatusReceived");
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella deserializzazione del messaggio status mezzo");
            }
        }

        /// <summary>
        /// Deserializza e propaga una risposta a comando.
        /// </summary>
        private async Task HandleRispostaComandoMessageAsync(int idParcheggio, string idMezzo, string payload, string topic)
        {
            try
            {
                var rispostaMessage = JsonSerializer.Deserialize<RispostaComandoMessage>(payload, _jsonOptions);

                if (rispostaMessage != null)
                {
                    _logger.LogInformation("Risposta comando ricevuta da mezzo {IdMezzo}: {Comando} -> {Successo}",
                        idMezzo, rispostaMessage.ComandoOriginale, rispostaMessage.Successo);

                    var eventArgs = new RispostaComandoReceivedEventArgs
                    {
                        IdParcheggio = idParcheggio,
                        IdMezzo = idMezzo,
                        RispostaMessage = rispostaMessage,
                        Topic = topic,
                        ReceivedAt = DateTime.UtcNow
                    };

                    try
                    {
                        RispostaComandoReceived?.Invoke(this, eventArgs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore in handler RispostaComandoReceived");
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella deserializzazione della risposta comando");
            }
        }

        /// <summary>
        /// Handler on-connect: pubblica stato online e sottoscrive i topic.
        /// </summary>
        private async Task OnMqttConnectedAsync(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation("Client MQTT connesso a {Host}:{Port}",
                _mqttConfig.BrokerHost, _mqttConfig.BrokerPort);

            var onlineMsg = new MqttApplicationMessageBuilder()
                .WithTopic("mobishare/backend/status")
                .WithPayload("{\"status\":\"online\"}")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true)
                .Build();

            await _mqttClient.EnqueueAsync(onlineMsg);
            await SottoscriviTopicsAsync();
        }

        /// <summary>
        /// Handler on-disconnect.
        /// </summary>
        private async Task OnMqttDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogWarning("Client MQTT disconnesso dal broker. Motivo: {Reason}", e.Reason);
            await Task.CompletedTask;
        }

        // =====================
        // DISPOSE PATTERN
        // =====================

        public void Dispose()
        {
            if (_disposed) return;

            _mqttClient.ApplicationMessageReceivedAsync -= OnMqttMessageReceivedAsync;
            _mqttClient.ConnectedAsync -= OnMqttConnectedAsync;
            _mqttClient.DisconnectedAsync -= OnMqttDisconnectedAsync;

            _mqttClient.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}