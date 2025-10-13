﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Mobishare.Infrastructure.Services;

namespace Mobishare.API.BackgroundServices
{
    /// <summary>
    /// Background Service che gira continuamente in background
    /// e controlla periodicamente le corse attive
    /// </summary>
    public class RideMonitoringBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RideMonitoringBackgroundService> _logger;

        // Intervallo di controllo: ogni quanti secondi controllare le corse
        // TODO: Rendere configurabile tramite appsettings.json
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

        public RideMonitoringBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<RideMonitoringBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _checkInterval = TimeSpan.FromSeconds(
                configuration.GetValue<int>("RideMonitoring:CheckIntervalSeconds", 30));
        }

        /// <summary>
        /// Metodo principale che viene eseguito quando il servizio parte
        /// e continua a girare finché l'applicazione non viene fermata
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Ride Monitoring Background Service avviato. Intervallo controllo: {IntervalloSecondi} secondi",
                _checkInterval.TotalSeconds);

            // Loop infinito che continua finché l'app non viene fermata
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1. Crea uno scope per risolvere le dipendenze scoped (come DbContext)
                    // I BackgroundService sono Singleton, ma DbContext deve essere Scoped
                    using var scope = _serviceProvider.CreateScope();

                    // 2. Ottieni il servizio di monitoraggio dallo scope
                    var monitoringService = scope.ServiceProvider
                        .GetRequiredService<IRideMonitoringService>();

                    // 3. Esegui il controllo delle corse
                    await monitoringService.CheckAndTerminateRidesAsync();
                }
                catch (Exception ex)
                {
                    // 4. Se c'è un errore, logga ma non fermare il servizio
                    // Il servizio continuerà a provare al prossimo ciclo
                    _logger.LogError(ex, "Errore durante il monitoraggio delle corse. Riproverò al prossimo ciclo");
                }

                // 5. Aspetta per l'intervallo specificato prima di ricontrollare
                // stoppingToken permette di interrompere l'attesa se l'app viene fermata
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Ride Monitoring Background Service terminato");
        }

        /// <summary>
        /// Metodo chiamato quando l'applicazione viene fermata
        /// Utile per cleanup o log finali
        /// </summary>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Arresto del servizio di monitoraggio in corso...");
            return base.StopAsync(cancellationToken);
        }
    }
}