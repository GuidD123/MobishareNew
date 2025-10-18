using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.Core.Enums;
using Mobishare.Core.Exceptions;
using Mobishare.Core.Models;
using Mobishare.Infrastructure.SignalRHubs;
using System;
using System.Threading.Tasks;

namespace Mobishare.Infrastructure.Services
{
    public class PagamentoService
    {
        private readonly MobishareDbContext _context;
        private readonly IHubContext<NotificheHub> _hubContext;
        private readonly ILogger<PagamentoService> _logger;

        public PagamentoService(
            MobishareDbContext context,
            IHubContext<NotificheHub> hubContext,
            ILogger<PagamentoService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Registra una movimentazione di credito (positiva o negativa)
        /// e aggiorna lo stato utente e le notifiche.
        /// </summary>
        public async Task<Transazione> RegistraMovimentoAsync(
            int idUtente,
            decimal importo,
            StatoPagamento stato,
            string tipo,
            int? idCorsa = null,
            int? idRicarica = null)
        {
            //Carica utente
            var utente = await _context.Utenti.FindAsync(idUtente)
                ?? throw new ElementoNonTrovatoException("Utente", idUtente);

            //Applica variazione di credito
            utente.Credito += importo; // importo può essere + o -
            if (utente.Credito < 0)
            {
                utente.Sospeso = true;
                _logger.LogWarning("Utente {Id} sospeso: credito negativo ({Credito})", utente.Id, utente.Credito);
            }

            //Crea record transazione
            var transazione = new Transazione
            {
                IdUtente = idUtente,
                Importo = importo,
                Stato = stato,
                Tipo = tipo,
                IdCorsa = idCorsa,
                IdRicarica = idRicarica,
                DataTransazione = DateTime.UtcNow
            };

            _context.Transazioni.Add(transazione);
            await _context.SaveChangesAsync();

            //Notifica SignalR all’utente
            await _hubContext.Clients.Group($"utenti:{idUtente}")
                .SendAsync("CreditoAggiornato", utente.Credito);

            await _hubContext.Clients.Group($"utenti:{idUtente}")
                .SendAsync("NuovaTransazione", new
                {
                    Tipo = tipo,
                    Importo = importo,
                    Stato = stato.ToString(),
                    Data = DateTime.UtcNow
                });

            //Notifica admin
            await _hubContext.Clients.Group("admin")
                .SendAsync("RiceviNotificaAdmin",
                    "Nuova transazione",
                    $"Utente {utente.Nome} (ID {utente.Id}) - {tipo}: {importo:+0.00;-0.00}€ ({stato})");

            _logger.LogInformation("Transazione registrata: Utente={Id}, Importo={Importo}, Tipo={Tipo}", idUtente, importo, tipo);

            return transazione;
        }
    }
}
