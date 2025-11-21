using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.IoT.Gateway.Services;

namespace Mobishare.IoT.Gateway.Services;

/// <summary>
/// Background service che sincronizza periodicamente i gateway emulatori con il database.
/// Gestisce automaticamente i trasferimenti di mezzi tra parcheggi.
/// </summary>
public class GatewaySyncBackgroundService : BackgroundService
{
    private readonly MqttGatewayManager _gatewayManager;
    private readonly ILogger<GatewaySyncBackgroundService> _logger;
    private readonly TimeSpan _syncInterval;

    public GatewaySyncBackgroundService(
        MqttGatewayManager gatewayManager,
        ILogger<GatewaySyncBackgroundService> logger)
    {
        _gatewayManager = gatewayManager;
        _logger = logger;
        _syncInterval = TimeSpan.FromSeconds(20); // Sincronizza ogni 30 secondi
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Gateway Sync Service avviato - Intervallo: {Interval} secondi",
            _syncInterval.TotalSeconds);

        // Aspetta 10 secondi prima del primo sync (dare tempo ai gateway di avviarsi)
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _gatewayManager.SincronizzaMezziConDatabaseAsync(stoppingToken);
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown richiesto, esci normalmente
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante sincronizzazione gateway");
                // Attendi comunque prima di riprovare
                await Task.Delay(_syncInterval, stoppingToken);
            }
        }

        _logger.LogInformation("Gateway Sync Service arrestato");
    }
}