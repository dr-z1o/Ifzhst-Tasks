using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetDummyApp;
public class NetSdrClient(ILogger logger = null)
{
    internal TcpClient? _tcpClient;
    internal NetworkStream? _stream;
    private readonly ILogger _logger = logger;

    public bool IsConnected => _tcpClient?.Connected == true;

    public async Task ConnectAsync(string host, int port = 50000)
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port);
            _stream = _tcpClient.GetStream();
            _logger?.LogInformation("Connected to {host}:{port}", host, port);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to {host}:{port}", host, port);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (IsConnected && _stream != null)
            {
                await _stream.FlushAsync();
                _stream.Close();
                _tcpClient.Close();
                _logger?.LogInformation("Disconnected from device");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during disconnect");
            throw;
        }
    }

    public async Task StartIqTransmissionAsync() => await SendCommandAsync("set RX On");
    public async Task StopIqTransmissionAsync() => await SendCommandAsync("set RX Off");
    public async Task SetFrequencyAsync(int freqHz) => await SendCommandAsync($"set RXFrequency {freqHz}");

    protected virtual NetworkStream GetStream() => _stream;

    protected virtual async Task SendCommandAsync(string command)
    {
        if (_stream == null || !_tcpClient.Connected)
        {
            _logger?.LogWarning("Attempted to send command while not connected");
            throw new InvalidOperationException("Not connected");
        }

        try
        {
            _logger?.LogInformation("Sending command: {command}", command);

            var bytes = Encoding.ASCII.GetBytes(command + "\n");
            await _stream.WriteAsync(bytes, 0, bytes.Length);

            var buffer = new byte[256];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            _logger?.LogInformation("Received response: {response}", response.Trim());

            if (response.StartsWith("NAK"))
                throw new InvalidOperationException($"Received NAK for command: {command}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send command: {command}", command);
            throw;
        }
    }
}