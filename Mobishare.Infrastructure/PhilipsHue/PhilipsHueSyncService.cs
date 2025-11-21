using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.Core.Enums;

namespace Mobishare.Infrastructure.PhilipsHue
{
    /// <summary>
    /// Servizio che sincronizza lo stato delle luci Philips Hue 
    /// con lo stato dei mezzi nel DB all'avvio dell'applicazione
    /// </summary>
    public class PhilipsHueSyncService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PhilipsHueSyncService> _logger;

        public PhilipsHueSyncService(
            IServiceProvider serviceProvider,
            ILogger<PhilipsHueSyncService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔄 Avvio sincronizzazione iniziale Philips Hue...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MobishareDbContext>();
                var hueControl = scope.ServiceProvider.GetRequiredService<PhilipsHueControl>();

                // Carica tutti i mezzi dal database
                var mezzi = await context.Mezzi
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                if (!mezzi.Any())
                {
                    _logger.LogInformation("Nessun mezzo trovato nel DB. Skip sincronizzazione Philips Hue.");
                    return;
                }

                _logger.LogInformation("Trovati {Count} mezzi. Sincronizzazione luci in corso...", mezzi.Count);

                int success = 0;
                int failed = 0;

                foreach (var mezzo in mezzi)
                {
                    try
                    {
                        var colore = GetColorePerStato(mezzo.Stato);
                        await hueControl.SetSpiaColor(mezzo.Matricola, colore);
                        
                        _logger.LogInformation(
                            "💡 Sync: Luce {Matricola} → {Colore} (Stato: {Stato})",
                            mezzo.Matricola, colore, mezzo.Stato);
                        
                        success++;
                        
                        // Piccolo delay per non sovraccaricare l'emulatore
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, 
                            "Impossibile sincronizzare luce per mezzo {Matricola}", 
                            mezzo.Matricola);
                        failed++;
                    }
                }

                _logger.LogInformation(
                    "✅ Sincronizzazione Philips Hue completata: {Success} successi, {Failed} fallimenti",
                    success, failed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "⚠️ Sincronizzazione Philips Hue fallita (non critico, continuo startup)");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔴 PhilipsHueSyncService terminato");
            return Task.CompletedTask;
        }

        private static ColoreSpia GetColorePerStato(StatoMezzo stato)
        {
            return stato switch
            {
                StatoMezzo.Disponibile => ColoreSpia.Verde,
                StatoMezzo.InUso => ColoreSpia.Blu,
                StatoMezzo.NonPrelevabile => ColoreSpia.Rosso,
                StatoMezzo.Manutenzione => ColoreSpia.Giallo,
                _ => ColoreSpia.Spenta
            };
        }
    }
}
