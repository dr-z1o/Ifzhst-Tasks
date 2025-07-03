using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NetDummyApp.Helper;
public class EmulatorServer(ILogger? logger = null, int tcpPort = 50000, int udpPort = 60000)
{
    private readonly TcpListener _listener = new(IPAddress.Any, tcpPort);
    private readonly int _tcpPort = tcpPort;
    private readonly int _udpPort = udpPort;
    private bool _transmitting;
    private CancellationTokenSource? _transmitCts;
    private readonly ILogger? _logger = logger;
    private readonly CancellationTokenSource _shutdownCts = new();

    public async Task StartAsync()
    {
        _listener.Start();
        _logger?.LogInformation("Emulator started on TCP {TcpPort}, UDP {UdpPort}", _tcpPort, _udpPort);

        try
        {
            while (!_shutdownCts.Token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_shutdownCts.Token);
                _logger?.LogInformation("Accepted new TCP client");
                await HandleClientAsync(client);

                // client disconnected => stop emulator
                _logger?.LogInformation("Client disconnected. Shutting down emulator.");
                StopIqUdpTransmission();
                _shutdownCts.Cancel();
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Emulator shutdown requested");
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using var stream = client.GetStream();
        var buffer = new byte[1024];

        while (client.Connected && !_shutdownCts.Token.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            string cmd = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            _logger?.LogInformation("[TCP] Received command: {Command}", cmd);

            string response = "ACK\n";

            if (cmd.Equals("set RX On", StringComparison.OrdinalIgnoreCase))
            {
                StartIqUdpTransmission();
            }
            else if (cmd.Equals("set RX Off", StringComparison.OrdinalIgnoreCase))
            {
                StopIqUdpTransmission();
            }

            await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
        }
    }

    private void StartIqUdpTransmission()
    {
        if (_transmitting) return;
        _transmitting = true;
        _transmitCts = new CancellationTokenSource();
        _ = Task.Run(() => TransmitIqAsync(_transmitCts.Token));
        _logger?.LogInformation("Started IQ transmission on UDP");
    }

    private void StopIqUdpTransmission()
    {
        if (!_transmitting) return;
        
        _transmitCts?.Cancel();
        _transmitting = false;
        _logger?.LogInformation("Stopped IQ transmission");
    }

    private async Task TransmitIqAsync(CancellationToken token)
    {
        using var udp = new UdpClient();
        var target = new IPEndPoint(IPAddress.Loopback, _udpPort);

        var rand = new Random();
        byte[] buffer = new byte[1024];

        while (!token.IsCancellationRequested)
        {
            rand.NextBytes(buffer);
            await udp.SendAsync(buffer, buffer.Length, target);
            _logger?.LogDebug("Sent UDP I/Q packet of {Length} bytes", buffer.Length);
            await Task.Delay(50, token); // simulate data rate
        }
    }
}