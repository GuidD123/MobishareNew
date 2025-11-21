using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mobishare.IoT.Gateway.Services;

/// <summary>
/// Background service che avvia e ferma tutti i gateway MQTT per i parcheggi attivi.
/// Questo è il service principale che gestisce il ciclo di vita dei gateway.
/// </summary>
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
        _logger.LogInformation("Avvio automatico Gateway MQTT per tutti i parcheggi attivi...");

        try
        {
            // Avvia gateway per TUTTI i parcheggi attivi nel database
            await _gatewayManager.AvviaTuttiGatewayAsync(cancellationToken);

            var count = _gatewayManager.ContaGatewayAttivi();
            _logger.LogInformation(
                "Gateway MQTT avviati con successo: {Count} gateway attivi",
                count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'avvio dei Gateway MQTT");
            throw; // Ferma l'applicazione se i gateway non partono
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Arresto di tutti i Gateway MQTT...");

        try
        {
            await _gatewayManager.FermaTuttiGatewayAsync(cancellationToken);
            _logger.LogInformation("Tutti i Gateway MQTT sono stati arrestati correttamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'arresto dei Gateway MQTT");
        }
    }
}
