using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.IoT.Gateway.Interfaces;

namespace Mobishare.IoT.Gateway.Services
{
    public class GatewayBackgroundService : BackgroundService
    {
        private readonly IMqttGatewayEmulatorService _gatewayService;
        private readonly ILogger<GatewayBackgroundService> _logger;

        public GatewayBackgroundService(
            IMqttGatewayEmulatorService gatewayService,
            ILogger<GatewayBackgroundService> logger)
        {
            _gatewayService = gatewayService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Gateway Background Service avviato");

            try
            {
                await _gatewayService.AvviaAsync();
                _logger.LogInformation("Gateway IoT connesso al broker MQTT");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel Gateway Background Service");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Gateway Background Service in arresto");
            await _gatewayService.FermaAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}