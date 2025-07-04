using System.Net.Sockets;
using System.Text;

namespace NetDummyApp;

/// <summary>
/// TcpNetworkClient is a simple implementation of INetworkClient that uses TcpClient
/// </summary>
public class TcpNetworkClient : INetworkClient
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;

    public bool IsConnected => _tcpClient?.Connected == true;

    private bool IsDisposed = false;

    public async Task ConnectAsync(string host, int port)
    {
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port);
        _stream = _tcpClient.GetStream();
    }

    public async Task DisconnectAsync()
    {
        if (IsConnected && _stream != null)
        {
            await _stream.FlushAsync();
            _stream.Close();
            _tcpClient?.Close();
        }
    }

    public async Task SendAsync(string command)
    {
        if (!IsConnected || _stream == null)
            throw new InvalidOperationException("Not connected to the server.");

        var bytes = Encoding.ASCII.GetBytes(command + "\n");
        await _stream.WriteAsync(bytes);
    }

    public async Task<string> ReceiveAsync()
    {
        if (!IsConnected || _stream == null)
            throw new InvalidOperationException("Not connected to the server.");

        var buffer = new byte[256];
        int bytesRead = await _stream.ReadAsync(buffer);
        return Encoding.ASCII.GetString(buffer, 0, bytesRead);
    }

    public Stream? GetStream() => _stream;

    public void Dispose()
    {
        if (IsDisposed) return;

        // Clean up resources
        IsDisposed = true;
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _tcpClient = null;
        _stream = null;
        GC.SuppressFinalize(this);
    }
}