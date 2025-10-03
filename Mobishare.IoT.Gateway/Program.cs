using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobishare.IoT.Gateway.Services;
using Mobishare.IoT.Gateway.Interfaces;
using Mobishare.IoT.Gateway.Config;

namespace Mobishare.IoT.Gateway
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("   Mobishare IoT Gateway - Avvio in corso...");
            Console.WriteLine("==============================================\n");

            var host = CreateHostBuilder(args).Build();

            Console.WriteLine("Gateway IoT avviato con successo!");
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
                    services.Configure<MqttConfiguration>(
                        context.Configuration.GetSection("Mqtt"));

                    services.AddSingleton<IMqttGatewayEmulatorService, MqttGatewayEmulatorService>();
                    services.AddHostedService<GatewayBackgroundService>();
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