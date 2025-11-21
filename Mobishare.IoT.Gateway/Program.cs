using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.Core.Data;
using Mobishare.IoT.Gateway.Services;
using Mobishare.IoT.Gateway.Config;

namespace Mobishare.IoT.Gateway
{
    /// <summary>
    /// IoT Gateway - Gestisce TUTTI i gateway MQTT per i parcheggi attivi.
    /// Questo è il componente centrale per la comunicazione IoT con i mezzi.
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         🚲 MOBISHARE IoT GATEWAY MULTI-PARKING 🛴          ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Avvio gateway MQTT per tutti i parcheggi attivi...");
            Console.WriteLine();

            var host = CreateHostBuilder(args).Build();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Gateway IoT avviato con successo!");
            Console.ResetColor();
            Console.WriteLine("Premi Ctrl+C per terminare.\n");

            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                        optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    // Configurazione MQTT
                    services.Configure<MqttConfiguration>(
                        context.Configuration.GetSection("Mqtt"));

                    // Database Context
                    services.AddDbContext<MobishareDbContext>(options =>
                        options.UseSqlite(
                            context.Configuration.GetConnectionString("DefaultConnection")));

                    // Multi-Gateway Manager (gestisce N gateway)
                    services.AddSingleton<MqttGatewayManager>();

                    // Background Services
                    services.AddHostedService<MqttGatewayHostedService>();
                    services.AddHostedService<GatewaySyncBackgroundService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
    }
}