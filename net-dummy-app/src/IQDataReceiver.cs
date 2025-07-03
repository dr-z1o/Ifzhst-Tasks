using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetDummyApp;

/// <summary>
/// Receives I/Q data over UDP and saves it to a file.
/// </summary>
public class IQDataReceiver
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _localEp;
    private readonly ILogger _logger;

    public IQDataReceiver(string address, int port, ILogger logger = null)
    {
        _localEp = new IPEndPoint(IPAddress.Parse(address), port);
        _udpClient = new UdpClient(_localEp);
        _logger = logger;
        _logger?.LogInformation("UDP receiver initialized on {address}:{port}", address, port);
    }

    public async Task StartReceivingAsync(string filePath, TimeSpan duration)
    {
        _logger?.LogInformation("Started receiving I/Q data to {filePath} for {duration} seconds", filePath, duration.TotalSeconds);

        try
        {
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            var cts = new CancellationTokenSource(duration);

            while (!cts.IsCancellationRequested)
            {
                if (_udpClient.Available > 0)
                {
                    var result = await _udpClient.ReceiveAsync();
                    bw.Write(result.Buffer);
                    _logger?.LogDebug("Received {length} bytes", result.Buffer.Length);
                }
                else
                {
                    await Task.Delay(10);
                }
            }

            _logger?.LogInformation("Finished receiving I/Q data");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error while receiving I/Q data");
            throw;
        }
    }
}