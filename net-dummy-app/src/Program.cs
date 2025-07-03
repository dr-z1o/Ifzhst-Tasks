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

        ILogger logger = loggerFactory.CreateLogger<Program>();

        var client = new NetSdrClient(logger);
        var iqLogger = loggerFactory.CreateLogger<IQDataReceiver>();
        try
        {
            await client.ConnectAsync("127.0.0.1");

            await client.SetFrequencyAsync(100_000_000); // 100 MHz
            await client.StartIqTransmissionAsync();

            var receiver = new IQDataReceiver("0.0.0.0", 60000, iqLogger);
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
