using Mobishare.IoT.Gateway.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mobishare.WebApp.Services;

public class MqttGatewayHostedService : IHostedService
{
    private readonly IMqttGatewayEmulatorService _gatewayService;
    private readonly ILogger<MqttGatewayHostedService> _logger;

    public MqttGatewayHostedService(
        IMqttGatewayEmulatorService gatewayService,
        ILogger<MqttGatewayHostedService> logger)
    {
        _gatewayService = gatewayService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Avvio automatico Gateway MQTT Emulator...");

        try
        {
            // Avvia il gateway per il parcheggio ID 1 (configurabile da appsettings se vuoi)
            await _gatewayService.StartAsync(idParcheggio: 1, cancellationToken);

            _logger.LogInformation("Gateway MQTT Emulator avviato con successo per parcheggio 1");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'avvio del Gateway MQTT Emulator");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Arresto Gateway MQTT Emulator...");

        try
        {
            await _gatewayService.StopAsync(cancellationToken);
            _logger.LogInformation("Gateway MQTT Emulator arrestato");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'arresto del Gateway MQTT Emulator");
        }
    }
}