using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Mobishare.Infrastructure.SignalRHubs
{
    /// <summary>
    /// Hub SignalR per gestire notifiche, telemetrie e aggiornamenti real-time.
    /// </summary>
    [Authorize]
    public class NotificheHub : Hub
    {
        private readonly ILogger<NotificheHub> _logger;

        public NotificheHub(ILogger<NotificheHub> logger)
        {
            _logger = logger;
        }

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

            _logger.LogInformation("Connessione SignalR: utente {UserId}, ruolo {Ruolo}, connId={ConnId}",
                userId, ruolo, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"utenti:{userId}");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin");

            _logger.LogInformation("Disconnessione SignalR: utente {UserId}, connId={ConnId}",
                userId, Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }


        // === METODI DI INVIO ===

        // Invio messaggio generico (test)
        public async Task InviaMessaggio(string user, string message)
        {
            await Clients.Group("admin").SendAsync("RiceviMessaggio", user, message);
        }

        // Aggiornamento credito utente
        public async Task InviaCreditoAggiornato(int userId, decimal nuovoCredito)
        {
            await Clients.Group($"utenti:{userId}")
                         .SendAsync("CreditoAggiornato", nuovoCredito);
        }

        // Aggiornamento telemetria di un mezzo (solo admin)
        public async Task InviaTelemetriaMezzo(object telemetria)
        {
            await Clients.Group("admin").SendAsync("AggiornamentoTelemetria", telemetria);
        }

        // Notifica per i gestori
        public async Task NotificaAdmin(string titolo, string testo)
        {
            await Clients.Group("admin")
                         .SendAsync("RiceviNotificaAdmin", titolo, testo);
        }

        // Notifica cambio stato utente
        public async Task NotificaAggiornamentoUtente(int userId, bool sospeso)
        {
            await Clients.Group("admin").SendAsync("AggiornamentoUtente", new
            {
                IdUtente = userId,
                Stato = sospeso ? "Sospeso" : "Attivo"
            });
        }

        public class DashboardData
        {
            public int NumeroCorseTotali { get; set; }
            public int CorseOggi { get; set; }
            public int CorseUltimaSettimana { get; set; }
            public int MezziDisponibili { get; set; }
            public int MezziInUso { get; set; }
            public int MezziGuasti { get; set; }
            public int UtentiTotali { get; set; }
            public int UtentiSospesi { get; set; }
        }

        // Aggiorna la dashboard admin
        public async Task AggiornaDashboard(DashboardData data)
        {
            await Clients.Group("admin").SendAsync("AggiornaDashboard", data);
        }
    }
}
