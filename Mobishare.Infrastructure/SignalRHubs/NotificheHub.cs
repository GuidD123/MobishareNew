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
                //await Clients.Caller.SendAsync("ConnessoAlGruppo", $"utenti:{userId}");
            }

            if (ruolo?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) == true)
            {
                // gruppo admin
                await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
                //await Clients.Caller.SendAsync("ConnessoAlGruppo", "admin");
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


        // ============================================================
        //                    NOTIFICHE UTENTE (solo alert)
        // ============================================================

        // Pagamento completato
        public async Task InviaPagamentoCompletato(int userId, decimal importo, string stato, decimal nuovoCredito)
        {
            await Clients.Group($"utenti:{userId}")
                .SendAsync("PagamentoCompletato", new
                {
                    importo,
                    stato,
                    nuovoCredito
                });
        }

        // Pagamento fallito
        public async Task InviaPagamentoFallito(int userId, decimal importo)
        {
            await Clients.Group($"utenti:{userId}")
                .SendAsync("PagamentoFallito", new
                {
                    importo
                });
        }

        // Aggiornamento credito utente
        public async Task InviaCreditoAggiornato(int userId, decimal nuovoCredito)
        {
            await Clients.Group($"utenti:{userId}")
                         .SendAsync("CreditoAggiornato", nuovoCredito);
        }

        // Account riattivato
        public async Task UtenteRiattivato(int userId, string nome, string messaggio)
        {
            await Clients.Group($"utenti:{userId}")
                .SendAsync("UtenteRiattivato", new
                {
                    nome,
                    messaggio
                });
        }

        // Aggiornamento corsa in tempo reale
        public async Task AggiornaCorsaUtente(int userId, object payload)
        {
            await Clients.Group($"utenti:{userId}")
                .SendAsync("AggiornaCorsa", payload);
        }

        // Utente sospeso (alert rosso + logout)
        public async Task NotificaUtenteSospeso(int userId, string messaggio)
        {
            await Clients.Group($"utenti:{userId}")
                .SendAsync("UtenteSospeso", new
                {
                    messaggio
                });
        }

        // Bonus applicato alla fine di una corsa
        public async Task InviaBonusApplicato(int userId, string messaggio, int totalePunti)
        {
            await Clients.Group($"utenti:{userId}")
                         .SendAsync("BonusApplicato", new
                         {
                             Messaggio = messaggio,
                             TotalePunti = totalePunti
                         });
        }

        // Bonus usato come sconto
        public async Task InviaBonusUsato(int userId, string messaggio)
        {
            await Clients.Group($"utenti:{userId}")
                         .SendAsync("BonusUsato", new
                         {
                             Messaggio = messaggio
                         });
        }


        // ============================================================
        //     NOTIFICHE ADMIN (SOLO CAMPANELLA)
        // ============================================================

        // Notifica cambio stato utente
        public async Task NotificaAggiornamentoUtente(int userId, bool sospeso)
        {
            await Clients.Group("admin")
                .SendAsync("UtenteSospesoAdmin", new
                {
                    IdUtente = userId,
                    Sospeso = sospeso
                });
        }

        //Telemetria critica (es. batteria < 20)
        public async Task NotificaTelemetriaCritica(object payload)
        {
            await Clients.Group("admin")
                .SendAsync("TelemetriaCritica", payload);
        }

        //Segnalazione guasto
        public async Task NotificaSegnalazioneGuasto(object payload)
        {
            await Clients.Group("admin")
                .SendAsync("SegnalazioneGuasto", payload);
        }

        public async Task NotificaAdmin(string titolo, string testo)
        {
            await Clients.Group("admin")
                .SendAsync("NotificaAdmin", new { Titolo = titolo, Testo = testo });
        }

        // Monitoraggio Sistema (MQTT, Gateway, Background Services)
        public async Task NotificaSistema(object payload)
        {
            await Clients.Group("admin")
                .SendAsync("SistemaStato", payload);
        }


        // ============================================================
        //     NOTIFICHE NON-NOTIFICA (non vanno nella campanella)
        // ============================================================

        // Aggiorna la dashboard admin
        public async Task AggiornaDashboard(DashboardData data)
        {
            await Clients.Group("admin").SendAsync("AggiornaDashboard", data);
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
    }
}
