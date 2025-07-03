using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NetDummyApp;

/// <summary>
/// Main entry point for the NetDummyApp application.
/// Connects to a NetSdr server, sets frequency, starts I/Q transmission,
/// and receives I/Q data over UDP.
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger<Program>();

        // Check if running in emulator mode
        // If the first argument is "moc", start the emulator server instead of the main flow
        if (args.Length > 0 && args[0].Equals("moc", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Starting in emulator mode (mock server)");
            var emulator = new Helper.EmulatorServer(loggerFactory.CreateLogger<Helper.EmulatorServer>());
            await emulator.StartAsync();
            return;
        }

        // proceed with the main flow
        using var tcpNetworkClient = new TcpNetworkClient();
        var client = new NetSdrClient(tcpNetworkClient, loggerFactory.CreateLogger<NetSdrClient>());

        try
        {
            await client.ConnectAsync("127.0.0.1");

            await client.SetFrequencyAsync(100_000_000); // 100 MHz
            await client.StartIqTransmissionAsync();

            var receiver = new IQDataReceiver("0.0.0.0", 60000, loggerFactory.CreateLogger<IQDataReceiver>());
            await receiver.StartReceivingAsync("iq_data.bin", TimeSpan.FromSeconds(5));

            await client.StopIqTransmissionAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in main flow");
        }
        finally
        {
            await client.DisconnectAsync();
        }
    }
}
