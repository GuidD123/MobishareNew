using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Mobishare.Infrastructure.SignalRHubs;
using System.Collections.Concurrent;

namespace Mobishare.Infrastructure.SignalRHubs.Services
{
    public class NotificationOutboxService
    {
        private readonly IHubContext<NotificheHub> _hubContext;
        private readonly ILogger<NotificationOutboxService> _logger;

        // Coda thread-safe
        private readonly ConcurrentQueue<(string userId, string eventName, object payload)> _queue = new();

        public NotificationOutboxService(IHubContext<NotificheHub> hubContext, ILogger<NotificationOutboxService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public void Enqueue(string userId, string eventName, object payload)
        {
            _queue.Enqueue((userId, eventName, payload));
        }

        public async Task FlushAsync()
        {
            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    await _hubContext.Clients.User(item.userId).SendAsync(item.eventName, item.payload);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invio notifica SignalR fallito. User={UserId}, Evento={Evento}", item.userId, item.eventName);
                    // Rimettila in coda per retry successivo
                    _queue.Enqueue(item);
                }
            }
        }
    }
}
