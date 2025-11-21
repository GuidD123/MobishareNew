using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Mobishare.IoT.Gateway.Interfaces;
using Mobishare.Core.Data;
using System.Collections.Concurrent;

namespace Mobishare.IoT.Gateway.Services;

public class MqttGatewayManager
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConcurrentDictionary<int, IMqttGatewayEmulatorService> _gateways = new();
    private readonly ILogger<MqttGatewayManager> _logger;

    public MqttGatewayManager(
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = loggerFactory.CreateLogger<MqttGatewayManager>();
    }

    /// <summary>
    /// Avvia un gateway per un parcheggio specifico e carica i mezzi dal database
    /// </summary>
    public async Task AvviaGatewayAsync(int idParcheggio, CancellationToken cancellationToken = default)
    {
        if (_gateways.ContainsKey(idParcheggio))
        {
            throw new InvalidOperationException($"Gateway per parcheggio {idParcheggio} già avviato");
        }

        var logger = _loggerFactory.CreateLogger<MqttGatewayEmulatorService>();
        var gateway = new MqttGatewayEmulatorService(logger, _configuration);

        // Avvia il gateway MQTT
        await gateway.StartAsync(idParcheggio, cancellationToken);
        _gateways.TryAdd(idParcheggio, gateway);

        _logger.LogInformation("Gateway MQTT avviato per parcheggio {IdParcheggio}", idParcheggio);

        // Carica i mezzi dal database per questo parcheggio
        await CaricaMezziDalDatabaseAsync(idParcheggio, gateway, cancellationToken);
    }

    /// <summary>
    /// Avvia gateway per TUTTI i parcheggi attivi nel database
    /// </summary>
    public async Task AvviaTuttiGatewayAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();

        // Trova tutti i parcheggi attivi
        var parcheggiAttivi = await dbContext.Parcheggi
            .Where(p => p.Attivo)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Avvio gateway per {Count} parcheggi attivi", parcheggiAttivi.Count);

        foreach (var idParcheggio in parcheggiAttivi)
        {
            try
            {
                await AvviaGatewayAsync(idParcheggio, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'avvio del gateway per parcheggio {IdParcheggio}", idParcheggio);
            }
        }

        _logger.LogInformation("Avvio gateway completato: {Count} gateway attivi", _gateways.Count);
    }

    /// <summary>
    /// Carica i mezzi dal database per un parcheggio specifico
    /// </summary>
    private async Task CaricaMezziDalDatabaseAsync(
        int idParcheggio,
        IMqttGatewayEmulatorService gateway,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();

        // Carica tutti i mezzi del parcheggio
        var mezzi = await dbContext.Mezzi
            .Where(m => m.IdParcheggioCorrente == idParcheggio)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Caricamento {Count} mezzi dal database per parcheggio {IdParcheggio}",
            mezzi.Count, idParcheggio);

        foreach (var mezzo in mezzi)
        {
            try
            {
                await gateway.AggiungiMezzoEmulato(
                    idMezzo: mezzo.Matricola,
                    matricola: mezzo.Matricola,
                    tipo: mezzo.Tipo,
                    statoIniziale: mezzo.Stato,
                    livelloBatteria: mezzo.LivelloBatteria
                );

                _logger.LogDebug(
                    "Mezzo {Matricola} caricato: Tipo={Tipo}, Stato={Stato}, Batteria={Batteria}%",
                    mezzo.Matricola, mezzo.Tipo, mezzo.Stato, mezzo.LivelloBatteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Errore nel caricamento del mezzo {Matricola} per parcheggio {IdParcheggio}",
                    mezzo.Matricola, idParcheggio);
            }
        }

        _logger.LogInformation(
            "Caricamento completato per parcheggio {IdParcheggio}: {Count} mezzi emulati",
            idParcheggio, gateway.GetMezziEmulati().Count);
    }

    /// <summary>
    /// Ferma un gateway specifico
    /// </summary>
    public async Task FermaGatewayAsync(int idParcheggio, CancellationToken cancellationToken = default)
    {
        if (_gateways.TryRemove(idParcheggio, out var gateway))
        {
            await gateway.StopAsync(cancellationToken);
            _logger.LogInformation("Gateway arrestato per parcheggio {IdParcheggio}", idParcheggio);
        }
    }

    /// <summary>
    /// Ferma tutti i gateway
    /// </summary>
    public async Task FermaTuttiGatewayAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Arresto di tutti i gateway ({Count} attivi)", _gateways.Count);

        foreach (var (idParcheggio, gateway) in _gateways)
        {
            try
            {
                await gateway.StopAsync(cancellationToken);
                _logger.LogInformation("Gateway arrestato per parcheggio {IdParcheggio}", idParcheggio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'arresto del gateway {IdParcheggio}", idParcheggio);
            }
        }

        _gateways.Clear();
        _logger.LogInformation("Tutti i gateway sono stati arrestati");
    }

    /// <summary>
    /// Ottieni il gateway per un parcheggio specifico
    /// </summary>
    public IMqttGatewayEmulatorService? GetGateway(int idParcheggio)
    {
        _gateways.TryGetValue(idParcheggio, out var gateway);
        return gateway;
    }

    /// <summary>
    /// Verifica se un gateway è attivo
    /// </summary>
    public bool IsGatewayAttivo(int idParcheggio)
    {
        return _gateways.TryGetValue(idParcheggio, out var gateway) && gateway.IsRunning;
    }

    /// <summary>
    /// Ottieni tutti i gateway attivi
    /// </summary>
    public IReadOnlyDictionary<int, IMqttGatewayEmulatorService> GetTuttiGateway()
    {
        return _gateways;
    }

    /// <summary>
    /// Conta i gateway attivi
    /// </summary>
    public int ContaGatewayAttivi() => _gateways.Count;

    /// <summary>
    /// Sincronizza i mezzi emulati con lo stato reale del database
    /// Rimuove mezzi che hanno cambiato parcheggio, aggiunge mezzi nuovi
    /// </summary>
    public async Task SincronizzaMezziConDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Avvio sincronizzazione gateway con database...");

        foreach (var (idParcheggio, gateway) in _gateways)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();

                // Carica mezzi dal DB per questo parcheggio
                var mezziDb = await dbContext.Mezzi
                    .Where(m => m.IdParcheggioCorrente == idParcheggio)
                    .Select(m => new { m.Matricola, m.Tipo, m.Stato, m.LivelloBatteria })
                    .ToListAsync(cancellationToken);

                var mezziEmulati = gateway.GetMezziEmulati().ToHashSet();
                var mezziDbMatricole = mezziDb.Select(m => m.Matricola).ToHashSet();

                // Rimuovi mezzi che non sono più in questo parcheggio
                // IMPORTANTE: NON rimuovere mezzi in stato InUso (potrebbero essere in transito tra parcheggi)
                var daRimuovere = mezziEmulati.Except(mezziDbMatricole).ToList();
                foreach (var matricola in daRimuovere)
                {
                    // Verifica lo stato del mezzo prima di rimuoverlo
                    var mezzoDb = await dbContext.Mezzi
                        .FirstOrDefaultAsync(m => m.Matricola == matricola, cancellationToken);
                    
                    // Se il mezzo è InUso, non rimuoverlo (è in corsa)
                    if (mezzoDb?.Stato == Core.Enums.StatoMezzo.InUso)
                    {
                        _logger.LogDebug("Mezzo {Matricola} in uso - sync saltata (corsa attiva)", matricola);
                        continue;
                    }
                    
                    await gateway.RimuoviMezzoEmulato(matricola);
                    _logger.LogInformation("Mezzo {Matricola} rimosso da parcheggio {IdParcheggio} (sync)",
                        matricola, idParcheggio);
                }

                // Aggiungi mezzi nuovi
                var daAggiungere = mezziDbMatricole.Except(mezziEmulati).ToList();
                foreach (var matricola in daAggiungere)
                {
                    var mezzo = mezziDb.First(m => m.Matricola == matricola);
                    await gateway.AggiungiMezzoEmulato(
                        idMezzo: mezzo.Matricola,
                        matricola: mezzo.Matricola,
                        tipo: mezzo.Tipo,
                        statoIniziale: mezzo.Stato,
                        livelloBatteria: mezzo.LivelloBatteria
                    );
                    _logger.LogInformation("Mezzo {Matricola} aggiunto a parcheggio {IdParcheggio} (sync)",
                        mezzo.Matricola, idParcheggio);
                }

                if (daRimuovere.Count == 0 && daAggiungere.Count == 0)
                {
                    _logger.LogDebug("Parcheggio {IdParcheggio}: nessuna modifica necessaria", idParcheggio);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore sincronizzazione parcheggio {IdParcheggio}", idParcheggio);
            }
        }

        _logger.LogDebug("Sincronizzazione completata per {Count} gateway", _gateways.Count);
    }

    /// <summary>
    /// Sincronizza un singolo mezzo dal database al gateway corretto (on-demand)
    /// Utile quando un comando MQTT arriva per un mezzo non presente nel gateway
    /// </summary>
    public async Task SincronizzaMezzoSingoloAsync(string matricola, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();

            var mezzo = await dbContext.Mezzi
                .FirstOrDefaultAsync(m => m.Matricola == matricola, cancellationToken);

            if (mezzo == null)
            {
                _logger.LogWarning("Mezzo {Matricola} non trovato nel database per sync on-demand", matricola);
                return;
            }

            if (mezzo.IdParcheggioCorrente == 0)
            {
                _logger.LogWarning("Mezzo {Matricola} non ha parcheggio assegnato", matricola);
                return;
            }

            var idParcheggio = mezzo.IdParcheggioCorrente;

            // Rimuovi il mezzo da tutti gli altri gateway
            foreach (var (gatewayId, gateway) in _gateways)
            {
                if (gatewayId != idParcheggio && gateway.GetMezziEmulati().Contains(matricola))
                {
                    await gateway.RimuoviMezzoEmulato(matricola);
                    _logger.LogInformation("Mezzo {Matricola} rimosso da parcheggio {IdParcheggio} (sync on-demand)",
                        matricola, gatewayId);
                }
            }

            // Aggiungi al gateway corretto se non già presente
            if (_gateways.TryGetValue(idParcheggio, out var targetGateway))
            {
                if (!targetGateway.GetMezziEmulati().Contains(matricola))
                {
                    await targetGateway.AggiungiMezzoEmulato(
                        idMezzo: mezzo.Matricola,
                        matricola: mezzo.Matricola,
                        tipo: mezzo.Tipo,
                        statoIniziale: mezzo.Stato,
                        livelloBatteria: mezzo.LivelloBatteria
                    );
                    _logger.LogInformation("Mezzo {Matricola} aggiunto a parcheggio {IdParcheggio} (sync on-demand)",
                        mezzo.Matricola, idParcheggio);
                }
            }
            else
            {
                _logger.LogWarning("Gateway per parcheggio {IdParcheggio} non trovato", idParcheggio);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante sync on-demand per mezzo {Matricola}", matricola);
        }
    }
}