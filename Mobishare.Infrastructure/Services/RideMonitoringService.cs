using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.Core.Models;
using Mobishare.Core.Enums;
using Microsoft.AspNetCore.SignalR;
using Mobishare.Infrastructure.SignalRHubs;

namespace Mobishare.Infrastructure.Services
{
    /// <summary>
    /// Interfaccia per il servizio di monitoraggio delle corse
    /// </summary>
    public interface IRideMonitoringService
    {
        Task CheckAndTerminateRidesAsync();
    }

    /// <summary>
    /// Servizio che controlla le corse attive e le termina automaticamente
    /// quando l'utente esaurisce il credito
    /// </summary>
    public class RideMonitoringService : IRideMonitoringService
    {
        private readonly MobishareDbContext _context;
        private readonly ILogger<RideMonitoringService> _logger;
        private readonly IHubContext<NotificheHub> _hubContext;

        public RideMonitoringService(
            MobishareDbContext context,
            ILogger<RideMonitoringService> logger,
            IHubContext<NotificheHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Controlla tutte le corse attive e termina quelle che hanno esaurito il credito
        /// </summary>
        public async Task CheckAndTerminateRidesAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var corseAttive = await (from c in _context.Corse
                                         join u in _context.Utenti on c.IdUtente equals u.Id
                                         join m in _context.Mezzi on c.MatricolaMezzo equals m.Matricola
                                         where c.DataOraFine == null || c.Stato == StatoCorsa.InCorso
                                         select new
                                         {
                                             Corsa = c,
                                             Utente = u,
                                             Mezzo = m
                                         }).ToListAsync();

                _logger.LogInformation("Controllando {Count} corse attive", corseAttive.Count);

                var corseTerminate = new List<Corsa>();

                foreach (var item in corseAttive)
                {
                    var corsa = item.Corsa;
                    var utente = item.Utente;
                    var mezzo = item.Mezzo;

                    //Calcola i minuti trascorsi
                    var startTime = corsa.DataOraInizio.Kind == DateTimeKind.Utc
                        ? corsa.DataOraInizio
                        : corsa.DataOraInizio.ToUniversalTime();

                    var durataMinuti = (DateTime.Now - startTime).TotalMinutes;

                    if (durataMinuti < 0)
                    {
                        _logger.LogError("Corsa {CorsaId} ha DataOraInizio nel futuro: {DataOraInizio}",
                            corsa.Id, corsa.DataOraInizio);
                        continue;
                    }

                    //Calcola il costo attuale
                    var costoAttuale = CalcolaCosto(durataMinuti, mezzo.Tipo);

                    //aggiornamento live tramite SignalR
                    await _hubContext.Clients.Group($"utenti:{utente.Id}")
                        .SendAsync("AggiornaCorsa", new
                        {
                            idCorsa = corsa.Id,
                            tipoMezzo = mezzo.Tipo.ToString(),
                            durataMinuti = Math.Round(durataMinuti, 1),
                            costoParziale = Math.Round(costoAttuale, 2)
                        });

                    //Verifica se il credito è insufficiente
                    if (costoAttuale >= utente.Credito)
                    {
                        _logger.LogWarning(
                            "Terminazione automatica corsa {CorsaId} - Utente: {NomeUtente} {CognomeUtente}, Credito: {Credito:C}, Costo calcolato: {Costo:C}, Durata: {Durata:F2} minuti",
                            corsa.Id,
                            utente.Nome,
                            utente.Cognome,
                            utente.Credito,
                            costoAttuale,
                            durataMinuti);

                        //Termina la corsa
                        corsa.DataOraFine = DateTime.Now;
                        corsa.Stato = StatoCorsa.Completata;
                        corsa.CostoFinale = utente.Credito;

                        //Gestione debito e sospensione utente
                        var debitoResiduo = costoAttuale - utente.Credito;
                        utente.DebitoResiduo += debitoResiduo;
                        utente.Credito = 0;
                        utente.Sospeso = true;

                        //Rendi il mezzo disponibile
                        var batteriaScarica = (mezzo.Tipo == TipoMezzo.BiciElettrica ||
                                              mezzo.Tipo == TipoMezzo.MonopattinoElettrico)
                                              && mezzo.LivelloBatteria < 20;

                        mezzo.Stato = batteriaScarica
                            ? StatoMezzo.NonPrelevabile
                            : StatoMezzo.Disponibile;

                        corseTerminate.Add(corsa);

                        _logger.LogInformation(
                            "Corsa {CorsaId} terminata. Mezzo {MatricolaMezzo} ora {StatoMezzo}. Utente sospeso con debito: {Debito:C}",
                            corsa.Id,
                            mezzo.Matricola,
                            mezzo.Stato,
                            debitoResiduo);
                    }
                }

                //Salva tutte le modifiche
                if (corseTerminate.Count > 0)
                {
                    var changesCount = await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Terminate {CountTerminate} corse. Salvate {CountChanges} modifiche al database",
                        corseTerminate.Count,
                        changesCount);
                }
                else
                {
                    await transaction.CommitAsync();
                    _logger.LogInformation("Nessuna corsa da terminare");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Errore durante il controllo delle corse attive. Rollback effettuato");
                throw;
            }
        }

        /// <summary>
        /// Calcola il costo della corsa seguendo la logica delle tariffe:
        /// - COSTO_BASE per i primi 30 minuti
        /// - Costo al minuto in base al tipo di mezzo per i minuti successivi
        /// </summary>
        private static decimal CalcolaCosto(double durataMinuti, TipoMezzo tipo)
        {
            decimal costo = Tariffe.COSTO_BASE;

            if (durataMinuti > 30)
            {
                var minutiExtra = (decimal)(durataMinuti - 30);

                switch (tipo)
                {
                    case TipoMezzo.MonopattinoElettrico:
                        costo += minutiExtra * Tariffe.COSTO_MINUTO_MONOPATTINOELETTRICO;
                        break;
                    case TipoMezzo.BiciElettrica:
                        costo += minutiExtra * Tariffe.COSTO_MINUTO_BICIELETTRICA;
                        break;
                    case TipoMezzo.BiciMuscolare:
                        costo += minutiExtra * Tariffe.COSTO_MINUTO_BICIMUSCOLARE;
                        break;
                }
            }

            return Math.Round(costo, 2, MidpointRounding.AwayFromZero);
        }
    }
}