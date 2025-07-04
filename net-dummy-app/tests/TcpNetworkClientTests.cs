using System.Net.Sockets;
using System.Text;
using Xunit;

namespace NetDummyApp.Tests;

public class TcpNetworkClientTests : IDisposable
{
    private readonly TcpNetworkClient _client;
    private readonly string _testHost = "127.0.0.1";
    private readonly int _testPort = 12345;

    public TcpNetworkClientTests()
    {
        _client = new TcpNetworkClient();
    }

    [Fact]
    public async Task ConnectAsync_ShouldConnect_WhenValidHostAndPort()
    {
        // Arrange
        using var server = TcpListener.Create(_testPort);
        server.Start();

        try
        {
            // Act
            await _client.ConnectAsync(_testHost, _testPort);

            // Assert
            Assert.True(_client.IsConnected);
        }
        finally
        {
            server.Stop();
        }
    }

    [Fact]
    public async Task DisconnectAsync_ShouldDisconnect_WhenConnected()
    {
        // Arrange
        using var server = TcpListener.Create(_testPort);
        server.Start();
        await _client.ConnectAsync(_testHost, _testPort);

        try
        {
            // Act
            await _client.DisconnectAsync();

            // Assert
            Assert.False(_client.IsConnected);
        }
        finally
        {
            server.Stop();
        }
    }

    [Fact]
    public async Task SendAsync_ShouldSendData_WhenConnected()
    {
        // Arrange
        using var server = TcpListener.Create(_testPort);
        server.Start();
        var message = "test message";
        var messageBytes = Encoding.ASCII.GetBytes(message + "\n");

        try
        {
            await _client.ConnectAsync(_testHost, _testPort);
            var serverClient = await server.AcceptTcpClientAsync();

            // Act
            await _client.SendAsync(message);

            // Assert
            var buffer = new byte[256];
            var bytesRead = await serverClient.GetStream().ReadAsync(buffer);
            var received = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Assert.Equal(message + "\n", received);
        }
        finally
        {
            server.Stop();
        }
    }

    [Fact]
    public async Task ReceiveAsync_ShouldReceiveData_WhenConnected()
    {
        // Arrange
        using var server = TcpListener.Create(_testPort);
        server.Start();
        var message = "test message";
        var messageBytes = Encoding.ASCII.GetBytes(message);

        try
        {
            await _client.ConnectAsync(_testHost, _testPort);
            var serverClient = await server.AcceptTcpClientAsync();

            // Act
            await serverClient.GetStream().WriteAsync(messageBytes);
            var received = await _client.ReceiveAsync();

            // Assert
            Assert.Equal(message, received);
        }
        finally
        {
            server.Stop();
        }
    }

    [Fact]
    public async Task SendAsync_ShouldThrow_WhenNotConnected()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _client.SendAsync("test"));
    }

    [Fact]
    public async Task ReceiveAsync_ShouldThrow_WhenNotConnected()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _client.ReceiveAsync());
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Act
        _client.Dispose();

        // Assert
        Assert.False(_client.IsConnected);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
