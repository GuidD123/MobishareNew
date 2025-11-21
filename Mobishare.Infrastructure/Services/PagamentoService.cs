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
            utente.Credito += importo;

            //NUOVO -> Chiama il metodo per aggiornare stato sospensione
            await AggiornaStatoSospensioneAsync(utente); 

            // Aggiorna sospensione in base al nuovo credito
            //if (utente.Credito <= 0)
            //{
            //    if (!utente.Sospeso)
            //    {
            //        utente.Sospeso = true;
            //        _logger.LogWarning("Utente {Id} sospeso per credito insufficiente ({Credito})", utente.Id, utente.Credito);
            //    }
            //}
            //else
            //{
            //    if (utente.Sospeso)
            //    {
            //        utente.Sospeso = false;
            //        _logger.LogInformation("Utente {Id} riattivato automaticamente (credito positivo: {Credito})", utente.Id, utente.Credito);
            //    }
            //}

            //Crea record transazione
            var transazione = new Transazione
            {
                IdUtente = idUtente,
                Importo = importo,
                Stato = stato,
                Tipo = tipo,
                IdCorsa = idCorsa,
                IdRicarica = idRicarica,
                DataTransazione = DateTime.Now
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
                    Data = DateTime.Now
                });

            //Notifica admin
            await _hubContext.Clients.Group("admin")
                .SendAsync("NotificaAdmin", new
                {
                    Titolo = "Nuova transazione",
                    Testo = $"Utente {utente.Nome} (ID {utente.Id}) - {tipo}: {importo:+0.00;-0.00}€ ({stato})"
                });

            _logger.LogInformation("Transazione registrata: Utente={Id}, Importo={Importo}, Tipo={Tipo}", idUtente, importo, tipo);

            return transazione;
        }

        /// <summary>
        /// Invia notifiche SignalR per un movimento completato.
        /// IMPORTANTE: Chiamare SOLO DOPO CommitAsync() per garantire coerenza UI/DB.
        /// </summary>
        public async Task InviaNotificheMovimentoAsync(
            int idUtente,
            decimal nuovoCredito,
            decimal importo,
            string tipo,
            StatoPagamento stato)
        {
            var utente = await _context.Utenti.FindAsync(idUtente);
            if (utente == null)
            {
                _logger.LogWarning("Impossibile inviare notifiche: Utente {Id} non trovato", idUtente);
                return;
            }

            try
            {
                // Notifica credito aggiornato
                await _hubContext.Clients.Group($"utenti:{idUtente}")
                    .SendAsync("CreditoAggiornato", nuovoCredito);

                // Notifica nuova transazione
                await _hubContext.Clients.Group($"utenti:{idUtente}")
                    .SendAsync("NuovaTransazione", new
                    {
                        Tipo = tipo,
                        Importo = importo,
                        Stato = stato.ToString(),
                        Data = DateTime.Now
                    });

                // Notifica admin
                await _hubContext.Clients.Group("admin")
                    .SendAsync("NotificaAdmin", new
                    {
                        Titolo = "Nuova transazione",
                        Testo = $"Utente {utente.Nome} (ID {utente.Id}) - {tipo}: {importo:+0.00;-0.00}€ ({stato})"
                    });

                _logger.LogInformation("Notifiche SignalR inviate per transazione: Utente={Id}, Tipo={Tipo}", idUtente, tipo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore invio notifiche SignalR per utente {Id}", idUtente);
                // Non propagare l'errore - le notifiche sono best-effort
            }
        }

        /// <summary>
        /// Aggiorna automaticamente lo stato di sospensione in base al credito.
        /// </summary>
        private async Task AggiornaStatoSospensioneAsync(Utente utente)
        {
            bool statoPrecedente = utente.Sospeso;
            utente.Sospeso = utente.Credito <= 0;

            if (statoPrecedente != utente.Sospeso)
            {
                await _context.SaveChangesAsync();

                string stato = utente.Sospeso ? "sospeso" : "riattivato";
                _logger.LogInformation("Utente {Id} {Stato} automaticamente (credito = {Credito})",
                    utente.Id, stato, utente.Credito);

                if (utente.Sospeso)
                {
                    try
                    {
                        //notifica utente
                        await _hubContext.Clients.Group($"utenti:{utente.Id}")
                            .SendAsync("UtenteSospeso", new
                            {
                                id = utente.Id, 
                                nome = utente.Nome, 
                                messaggio = "Il tuo account è stato sospeso per credito insufficiente."
                            });

                        //Notifica admin
                        await _hubContext.Clients.Group("admin")
                            .SendAsync("NotificaAdmin", new
                            {
                                Titolo = "Utente sospeso",
                                Testo = $"L'utente {utente.Nome} (ID {utente.Id}) è stato sospeso per credito insufficiente."
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "SignalR: impossibile inviare notifica sospensione utente {Id}", utente.Id);
                    }
                }
                else
                {
                    try
                    {
                        //notifica utente
                        await _hubContext.Clients.Group($"utenti:{utente.Id}")
                            .SendAsync("UtenteRiattivato", new
                            {
                                nome = utente.Nome,
                                messaggio = "Il tuo account è stato riattivato. Puoi nuovamente usare Mobishare."
                            });

                        //Notifica admin
                        await _hubContext.Clients.Group("admin")
                            .SendAsync("NotificaAdmin", new 
                            {
                                Titolo = "Utente riattivato",
                                Testo = $"L'utente {utente.Nome} (ID {utente.Id}) è stato riattivato automaticamente."
                            });
                               
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "SignalR: impossibile inviare notifica riattivazione utente {Id}", utente.Id);
                    }
                }
            }
        }
    }
}
