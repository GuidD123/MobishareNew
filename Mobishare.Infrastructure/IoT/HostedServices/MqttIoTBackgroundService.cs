using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.Infrastructure.IoT.Interfaces;

namespace Mobishare.Infrastructure.IoT.HostedServices
{
    /// <summary>
    /// Servizio in background che ascolta i messaggi MQTT
    /// e aggiorna il database Mobishare di conseguenza.
    /// </summary>
    public class MqttIoTBackgroundService(
        IMqttIoTService iotService,
        IServiceProvider services,
        ILogger<MqttIoTBackgroundService> logger) : BackgroundService
    {
        private readonly IMqttIoTService _iotService = iotService;
        private readonly IServiceProvider _services = services;
        private readonly ILogger<MqttIoTBackgroundService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Avvio connessione MQTT
            await _iotService.StartAsync(stoppingToken);

            // Aggancio listener agli eventi
            _iotService.MezzoStatusReceived += async (sender, e) =>
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();

                    var mezzo = await db.Mezzi
                        .FirstOrDefaultAsync(m => m.Matricola == e.StatusMessage.Matricola, stoppingToken);

                    if (mezzo != null)
                    {
                        mezzo.Stato = e.StatusMessage.Stato;
                        mezzo.LivelloBatteria = e.StatusMessage.LivelloBatteria;

                        db.Update(mezzo);
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore aggiornando il DB da MQTT");
                }
            };

            _iotService.RispostaComandoReceived += (sender, e) =>
            {
                _logger.LogInformation("Risposta comando: Mezzo={Mezzo}, Successo={Successo}",
                    e.IdMezzo, e.RispostaMessage.Successo);
            };
        }
    }
}
