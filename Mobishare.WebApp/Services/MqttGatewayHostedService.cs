using Mobishare.IoT.Gateway.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mobishare.WebApp.Services;

public class MqttGatewayHostedService : IHostedService
{
    private readonly MqttGatewayManager _gatewayManager;
    private readonly ILogger<MqttGatewayHostedService> _logger;

    public MqttGatewayHostedService(
        MqttGatewayManager gatewayManager,
        ILogger<MqttGatewayHostedService> logger)
    {
        _gatewayManager = gatewayManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Avvio automatico Gateway MQTT Emulators per tutti i parcheggi attivi...");

        try
        {
            // Avvia gateway per TUTTI i parcheggi attivi nel database
            await _gatewayManager.AvviaTuttiGatewayAsync(cancellationToken);

            _logger.LogInformation(
                "Gateway MQTT Emulators avviati con successo: {Count} gateway attivi",
                _gatewayManager.ContaGatewayAttivi());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'avvio dei Gateway MQTT Emulators");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Arresto di tutti i Gateway MQTT Emulators...");

        try
        {
            await _gatewayManager.FermaTuttiGatewayAsync(cancellationToken);
            _logger.LogInformation("Tutti i Gateway MQTT Emulators sono stati arrestati");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'arresto dei Gateway MQTT Emulators");
        }
    }
}