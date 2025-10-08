using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mobishare.Infrastructure.SignalRHubs
{
    /// <summary>
    /// Hub SignalR per gestire notifiche, telemetrie e aggiornamenti real-time.
    /// </summary>
    [Authorize]
    public class NotificheHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ruolo = user?.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // gruppo personale utente
                await Groups.AddToGroupAsync(Context.ConnectionId, $"utenti:{userId}");
                await Clients.Caller.SendAsync("ConnessoAlGruppo", $"utenti:{userId}");
            }

            if (ruolo?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) == true)
            {
                // gruppo admin
                await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
                await Clients.Caller.SendAsync("ConnessoAlGruppo", "admin");
            }

            Console.WriteLine($"Connessione SignalR: utente {userId}, ruolo {ruolo}, connId={Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"utenti:{userId}");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin");

            Console.WriteLine($"Disconnessione SignalR: utente {userId}, connId={Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        // === METODI DI INVIO ===

        // Invio messaggio generico (test)
        public async Task InviaMessaggio(string user, string message)
        {
            await Clients.All.SendAsync("RiceviMessaggio", user, message);
        }

        // Aggiornamento credito utente
        public async Task InviaCreditoAggiornato(int userId, decimal nuovoCredito)
        {
            await Clients.Group($"utenti:{userId}")
                         .SendAsync("CreditoAggiornato", nuovoCredito);
        }

        // Aggiornamento telemetria di un mezzo
        public async Task InviaTelemetriaMezzo(object telemetria)
        {
            await Clients.All.SendAsync("AggiornamentoTelemetria", telemetria);
        }

        // Notifica per i gestori
        public async Task NotificaAdmin(string titolo, string testo)
        {
            await Clients.Group("admin")
                         .SendAsync("RiceviNotificaAdmin", titolo, testo);
        }
    }
}
