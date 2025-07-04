using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace NetDummyApp;

/// <summary>
/// Interface for UdpClient to allow dependency injection and testing.
/// This interface abstracts the UdpClient functionality to enable mocking in tests.
/// </summary>
public interface IUdpClient : IDisposable
{
    int Available { get; }
    Task<UdpReceiveResult> ReceiveAsync();
    void Close();
}

/// <summary>
/// Wrapper for UdpClient to allow dependency injection and testing.
/// This interface abstracts the UdpClient functionality to enable mocking in tests.
/// </summary>
public class UdpClientWrapper : IUdpClient
{
    private readonly UdpClient _udpClient;

    public UdpClientWrapper(IPEndPoint endPoint)
    {
        _udpClient = new UdpClient();
        _udpClient.ExclusiveAddressUse = false;
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.Bind(endPoint);
    }

    public int Available => _udpClient.Available;

    public Task<UdpReceiveResult> ReceiveAsync() => _udpClient.ReceiveAsync();

    public void Close() => _udpClient.Close();

    public void Dispose()
    {
        _udpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}



/// <summary>
/// Receives I/Q data over UDP and saves it to a file.
/// </summary>
public class IQDataReceiver : IDisposable
{
    private readonly IUdpClient? _udpClient;
    private readonly IPEndPoint? _localEp;
    private readonly ILogger? _logger;
    private bool IsDisposed = false;

    public IQDataReceiver(string address, int port, ILogger? logger = null)
                : this(new UdpClientWrapper(new IPEndPoint(IPAddress.Parse(address), port)), logger)
    {
    }

    // Constructor for testing
    internal IQDataReceiver(IUdpClient udpClient, ILogger? logger = null)
    {
        _udpClient = udpClient;
        _logger = logger;
        _logger?.LogInformation("UDP receiver initialized");
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
                if (_udpClient?.Available > 0)
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

    public void Dispose()
    {
        if (IsDisposed) return;

        _udpClient?.Dispose();
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
}