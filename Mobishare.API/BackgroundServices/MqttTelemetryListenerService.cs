/*using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.Core.Enums;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.SignalRHubs;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
using System.Text.Json;

namespace Mobishare.API.BackgroundServices;

public class MqttTelemetryListenerService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MqttTelemetryListenerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<NotificheHub> _hubContext;
    private readonly IManagedMqttClient _mqttClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed = false;

    public MqttTelemetryListenerService(
        IServiceProvider serviceProvider,
        ILogger<MqttTelemetryListenerService> logger,
        IConfiguration configuration,
        IHubContext<NotificheHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _hubContext = hubContext;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var factory = new MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();

        _mqttClient.ApplicationMessageReceivedAsync += OnTelemetryReceivedAsync;
        _mqttClient.ConnectedAsync += OnConnectedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Avvio MQTT Telemetry Listener Service...");

            var brokerHost = _configuration["Mqtt:BrokerHost"] ?? "localhost";
            var brokerPort = _configuration.GetValue<int>("Mqtt:BrokerPort", 1883);
            var username = _configuration["Mqtt:Username"];
            var password = _configuration["Mqtt:Password"];
            var useTls = _configuration.GetValue<bool>("Mqtt:UseTls", false);

            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId("MobishareTelemetryListener-" + Environment.MachineName + "-" + DateTime.Now.Ticks)
                .WithTcpServer(brokerHost, brokerPort)
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60));

            if (!string.IsNullOrEmpty(username))
            {
                clientOptions.WithCredentials(username, password);
            }

            if (useTls)
            {
                clientOptions.WithTlsOptions(o => o.UseTls());
            }

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(clientOptions.Build())
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .Build();

            await _mqttClient.StartAsync(managedOptions);

            // Sottoscrivi al topic telemetria: Parking/+/Mezzi/+
            var telemetryTopic = "Parking/+/Mezzi/+";
            await _mqttClient.SubscribeAsync(new[]
            {
                new MqttTopicFilterBuilder()
                    .WithTopic(telemetryTopic)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build()
            });

            _logger.LogInformation("MQTT Telemetry Listener sottoscritto al topic: {Topic}", telemetryTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'avvio di MQTT Telemetry Listener");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Arresto MQTT Telemetry Listener Service...");
            await _mqttClient.StopAsync();
            _logger.LogInformation("MQTT Telemetry Listener arrestato");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'arresto di MQTT Telemetry Listener");
        }
    }

    private async Task OnTelemetryReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            _logger.LogDebug("Telemetria ricevuta su topic {Topic}: {Payload}", topic, payload);

            // Parse topic: Parking/{idParcheggio}/Mezzi/{idMezzo}
            var topicParts = topic.Split('/');
            if (topicParts.Length != 4 || topicParts[0] != "Parking" || topicParts[2] != "Mezzi")
            {
                _logger.LogWarning("Topic telemetria non valido: {Topic}", topic);
                return;
            }

            var idMezzoStr = topicParts[3];

            // Deserializza il messaggio
            var statusMessage = JsonSerializer.Deserialize<MezzoStatusMessage>(payload, _jsonOptions);
            if (statusMessage == null)
            {
                _logger.LogWarning("Impossibile deserializzare messaggio telemetria da topic {Topic}", topic);
                return;
            }

            // Aggiorna il database
            await AggiornaDatabaseAsync(idMezzoStr, statusMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella gestione della telemetria ricevuta");
        }
    }

    private async Task AggiornaDatabaseAsync(string idMezzoStr, MezzoStatusMessage statusMessage)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();

            // Trova il mezzo per matricola (non per ID, perché il gateway usa la matricola)
            var mezzo = await dbContext.Mezzi
                .FirstOrDefaultAsync(m => m.Matricola == statusMessage.Matricola);

            if (mezzo == null)
            {
                _logger.LogWarning("Mezzo con matricola {Matricola} non trovato nel database", statusMessage.Matricola);
                return;
            }

            // Salva i vecchi valori per confronto
            var vecchioLivelloBatteria = mezzo.LivelloBatteria;
            var vecchioStato = mezzo.Stato;

            // Aggiorna i valori dal messaggio
            mezzo.LivelloBatteria = statusMessage.LivelloBatteria;
            mezzo.Stato = statusMessage.Stato;

            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Mezzo {Matricola} aggiornato: Batteria {VecchiaBatteria}% → {NuovaBatteria}%, Stato {VecchioStato} → {NuovoStato}",
                mezzo.Matricola,
                vecchioLivelloBatteria,
                mezzo.LivelloBatteria,
                vecchioStato,
                mezzo.Stato);

            //NOTIFICA ADMIN: Batteria critica
            if (mezzo.LivelloBatteria < 20 && vecchioLivelloBatteria >= 20)
            {
                try
                {
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("NotificaAdmin", new {
                            Titolo = "Batteria Critica",
                            Testo = $"Il mezzo {mezzo.Matricola} ({mezzo.Tipo}) ha raggiunto il {mezzo.LivelloBatteria}% di batteria durante la corsa. Richiede ricarica urgente."
                        });

                    _logger.LogWarning("Notifica admin inviata: batteria critica per mezzo {Matricola} ({Livello}%)",
                        mezzo.Matricola, mezzo.LivelloBatteria);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Impossibile inviare notifica batteria critica per mezzo {Matricola}", mezzo.Matricola);
                }
            }

            //NOTIFICA ADMIN: Mezzo diventa non prelevabile
            if (mezzo.Stato == StatoMezzo.NonPrelevabile && vecchioStato != StatoMezzo.NonPrelevabile)
            {
                try
                {
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("NotificaAdmin", new {
                            Titolo = "Mezzo Non Prelevabile",
                            Testo = $"Il mezzo {mezzo.Matricola} ({mezzo.Tipo}) è diventato non prelevabile (batteria: {mezzo.LivelloBatteria}%)"
                        });

                    _logger.LogWarning("Notifica admin inviata: mezzo {Matricola} non prelevabile", mezzo.Matricola);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Impossibile inviare notifica mezzo non prelevabile {Matricola}", mezzo.Matricola);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'aggiornamento del database per mezzo {IdMezzo}", idMezzoStr);
        }
    }

    private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        _logger.LogInformation("MQTT Telemetry Listener connesso al broker MQTT");
        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        _logger.LogWarning("MQTT Telemetry Listener disconnesso dal broker MQTT: {Reason}", e.Reason);
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
}*/