using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.Core.Enums;
using Mobishare.Infrastructure.IoT.Events;
using Mobishare.Infrastructure.IoT.Interfaces;
using Mobishare.Infrastructure.SignalRHubs;
using System;
using System.Threading;
using System.Threading.Tasks;


///<summary>
/// Servizio applicativo che usa gli eventi di MqttIoTService per aggiornare il db o altri sistemi 
/// Livello: logica applicativa / sincronizzazione dati 
/// 
/// E' un consumer degli eventi generati da MqttIoTService:
///     Si sottoscrive a: 
///         _iotService.MezzoStatusReceived += OnMezzoStatusReceived;
///        _iotService.RispostaComandoReceived += OnRispostaComandoReceived;
///     e quando arriva un evento crea uno scope di servizio, apre il DbContext e aggiorna la riga del mezzo nel DB con i nuovi valori di stato e livello batteria 
///     
///     Logica di sincronizzazione tra IoT e dominio applicativo Db
///    
/// </summary>

namespace Mobishare.Infrastructure.IoT.HostedServices
{
    /// <summary>
    /// Ascolta eventi MQTT e sincronizza il DB.
    /// </summary>
    public sealed class MqttIoTBackgroundService : BackgroundService
    {
        private readonly IMqttIoTService _iotService;
        private readonly IServiceProvider _services;
        private readonly ILogger<MqttIoTBackgroundService> _logger;
        private readonly IHubContext<NotificheHub> _hubContext;

        // mantengo il token per usarlo negli handler
        private CancellationToken _stoppingToken;

        public MqttIoTBackgroundService(
            IMqttIoTService iotService,
            IServiceProvider services,
            IHubContext<NotificheHub> hubContext,
            ILogger<MqttIoTBackgroundService> logger)
        {
            _iotService = iotService;
            _services = services;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;

            // subscribe
            _iotService.MezzoStatusReceived += OnMezzoStatusReceived;
            _iotService.RispostaComandoReceived += OnRispostaComandoReceived;

            try
            {
                // resta in vita finché non viene cancellato
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutdown richiesto
            }
            finally
            {
                // unsubscribe per evitare memory leak
                _iotService.MezzoStatusReceived -= OnMezzoStatusReceived;
                _iotService.RispostaComandoReceived -= OnRispostaComandoReceived;
            }
        }

        private void OnMezzoStatusReceived(object? sender, MezzoStatusReceivedEventArgs e)
        {
            // non bloccare il thread evento: esegui async in background
            _ = Task.Run(() => HandleMezzoStatusAsync(e, _stoppingToken), _stoppingToken);
        }

        private async Task HandleMezzoStatusAsync(MezzoStatusReceivedEventArgs e, CancellationToken ct)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();

                // NB: nel payload il campo è IdMezzo (coerente con MqttIoTService)
                var mezzo = await db.Mezzi
                    .FirstOrDefaultAsync(m => m.Matricola == e.StatusMessage.Matricola, ct);

                if (mezzo is null)
                {
                    _logger.LogWarning("Mezzo non trovato per telemetria. Matricola={Matricola}", e.StatusMessage.Matricola);
                    return;
                }

                // Filtra le bici muscolari - non devono avere batteria
                if (mezzo.Tipo == Core.Enums.TipoMezzo.BiciMuscolare)
                {
                    // Aggiorna solo lo stato, NON la batteria
                    mezzo.Stato = e.StatusMessage.Stato;
                    mezzo.LivelloBatteria = null; // Forza null per le bici muscolari
                    _logger.LogDebug("Aggiornamento bici muscolare {Matricola}: solo stato {Stato}",
                        mezzo.Matricola, mezzo.Stato);
                }
                else
                {
                    // Mezzi elettrici: aggiorna stato e batteria
                    // Aggiorna solo se la batteria è cambiata (ottimizzazione)
                    bool batteriaChanged = mezzo.LivelloBatteria != e.StatusMessage.LivelloBatteria;
                    bool statoChanged = mezzo.Stato != e.StatusMessage.Stato;

                    if (!batteriaChanged && !statoChanged)
                    {
                        _logger.LogDebug("Nessun cambiamento per mezzo {Matricola}, skip update", mezzo.Matricola);
                        return;
                    }

                    // Se il DB dice Disponibile ma il gateway dice InUso, ignora
                    if (mezzo.Stato == StatoMezzo.Disponibile && e.StatusMessage.Stato == StatoMezzo.InUso)
                    {
                        _logger.LogWarning("Ignorato status InUso per mezzo {Matricola} - DB dice già Disponibile",
                            mezzo.Matricola);
                        return;
                    }

                    mezzo.Stato = e.StatusMessage.Stato;
                    mezzo.LivelloBatteria = e.StatusMessage.LivelloBatteria;
                }

                await db.SaveChangesAsync(ct);

                //invio notifica signalR -  telemetria in tempo reale
                await _hubContext.Clients.All.SendAsync("AggiornamentoTelemetria", new
                {
                    mezzo.Matricola,
                    mezzo.Stato,
                    mezzo.LivelloBatteria,
                    e.IdParcheggio,
                    TimeStamp = DateTime.Now
                }, ct);

                _logger.LogInformation("Telemetria inoltrata via SignalR per mezzo {Matricola}", mezzo.Matricola);
            }
            catch (OperationCanceledException)
            {
                // chiusura in corso
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore aggiornando il DB da MQTT");
            }
        }

        private void OnRispostaComandoReceived(object? sender, RispostaComandoReceivedEventArgs e)
        {
            _logger.LogInformation("Risposta comando: Mezzo={Mezzo} Successo={Successo} CmdId={CmdId}",
                e.IdMezzo, e.RispostaMessage.Successo, e.RispostaMessage.CommandId);

            // Inoltra eventuali risposte anche ai client (opzionale)
            _ = _hubContext.Clients.All.SendAsync("RispostaComando", new
            {
                e.IdMezzo,
                e.RispostaMessage.Successo,
                e.RispostaMessage.ComandoOriginale,
                e.RispostaMessage.CommandId
            });
        }
    }
}
