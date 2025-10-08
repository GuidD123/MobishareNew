using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.Infrastructure.SignalRHubs.Services;

namespace Mobishare.Infrastructure.SignalRHubs.HostedServices
{
    public class NotificationRetryService : BackgroundService
    {
        private readonly NotificationOutboxService _outbox;
        private readonly ILogger<NotificationRetryService> _logger;

        public NotificationRetryService(NotificationOutboxService outbox, ILogger<NotificationRetryService> logger)
        {
            _outbox = outbox;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servizio retry notifiche SignalR avviato");

            while (!stoppingToken.IsCancellationRequested)
            {
                await _outbox.FlushAsync();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("Servizio retry notifiche SignalR terminato");
        }
    }
}
