using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.Infrastructure.IoT.Events;
using Mobishare.Infrastructure.IoT.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mobishare.Infrastructure.IoT.HostedServices
{
    /// <summary>
    /// Ascolta eventi MQTT e sincronizza il DB.
    /// </summary>
    public sealed class MqttIoTBackgroundService : BackgroundService
    {
        private readonly IMqttIoTService _iotService;
        private readonly IServiceProvider _services;
        private readonly ILogger<MqttIoTBackgroundService> _logger;

        // mantengo il token per usarlo negli handler
        private CancellationToken _stoppingToken;

        public MqttIoTBackgroundService(
            IMqttIoTService iotService,
            IServiceProvider services,
            ILogger<MqttIoTBackgroundService> logger)
        {
            _iotService = iotService;
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;

            // subscribe
            _iotService.MezzoStatusReceived += OnMezzoStatusReceived;
            _iotService.RispostaComandoReceived += OnRispostaComandoReceived;

            try
            {
                // resta in vita finché non viene cancellato
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutdown richiesto
            }
            finally
            {
                // unsubscribe per evitare memory leak
                _iotService.MezzoStatusReceived -= OnMezzoStatusReceived;
                _iotService.RispostaComandoReceived -= OnRispostaComandoReceived;
            }
        }

        private void OnMezzoStatusReceived(object? sender, MezzoStatusReceivedEventArgs e)
        {
            // non bloccare il thread evento: esegui async in background
            _ = Task.Run(() => HandleMezzoStatusAsync(e, _stoppingToken), _stoppingToken);
        }

        private async Task HandleMezzoStatusAsync(MezzoStatusReceivedEventArgs e, CancellationToken ct)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();

                // NB: nel payload il campo è IdMezzo (coerente con MqttIoTService)
                var mezzo = await db.Mezzi
                    .FirstOrDefaultAsync(m => m.Matricola == e.StatusMessage.Matricola, ct);

                if (mezzo is null)
                {
                    _logger.LogWarning("Mezzo non trovato per telemetria. Matricola={Matricola}", e.StatusMessage.Matricola);
                    return;
                }

                mezzo.Stato = e.StatusMessage.Stato;
                mezzo.LivelloBatteria = e.StatusMessage.LivelloBatteria;

                await db.SaveChangesAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // chiusura in corso
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore aggiornando il DB da MQTT");
            }
        }

        private void OnRispostaComandoReceived(object? sender, RispostaComandoReceivedEventArgs e)
        {
            _logger.LogInformation("Risposta comando: Mezzo={Mezzo} Successo={Successo} CmdId={CmdId}",
                e.IdMezzo, e.RispostaMessage.Successo, e.RispostaMessage.CommandId);
        }
    }
}
