using Microsoft.Extensions.Logging;

namespace NetDummyApp;

/// <summary>
/// Interface for network client to allow dependency injection and testing.
/// This interface abstracts the network client functionality to enable mocking in tests.
/// </summary>
public interface INetworkClient : IDisposable
{
    Task ConnectAsync(string host, int port);
    Task DisconnectAsync();
    Task SendAsync(string command);
    Task<string> ReceiveAsync();
    Stream? GetStream();
    bool IsConnected { get; }
}

/// <summary>
/// Wrapper for network client to allow dependency injection and testing.
/// </summary>
public class NetSdrClient(INetworkClient networkClient, ILogger? logger = null)
{
    internal readonly INetworkClient? _networkClient = networkClient;
    private readonly ILogger? _logger = logger;

    public bool IsConnected => _networkClient?.IsConnected == true;

    private void ThrowIfNotInitialized(string message)
    {
        if (_networkClient is null)
        {
            _logger?.LogError(message);
            throw new InvalidOperationException(message);
        }
    }

    public async Task ConnectAsync(string host, int port = 50000)
    {
        try
        {
            if (_networkClient is null)
            {
                _logger?.LogError("Network client is not initialized");
                throw new InvalidOperationException("Network client is not initialized");
            }
            await _networkClient.ConnectAsync(host, port);
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
            if (_networkClient is null)
            {
                _logger?.LogError("Network client is not initialized");
                throw new InvalidOperationException("Network client is not initialized");
            }
            await _networkClient.DisconnectAsync();
            _logger?.LogInformation("Disconnected from device");
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

    private async Task SendCommandAsync(string command)
    {
        if (_networkClient is null || !_networkClient.IsConnected)
        {
            _logger?.LogWarning("Attempted to send command while not connected");
            throw new InvalidOperationException("Not connected");
        }

        try
        {
            _logger?.LogInformation("Sending command: {command}", command);
            await _networkClient.SendAsync(command);
            var response = await _networkClient.ReceiveAsync();

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