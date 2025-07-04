using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NetDummyApp.Tests;

public class NetSdrClientTests
{
    private readonly Mock<INetworkClient> _mockNetworkClient;
    private readonly Mock<ILogger<NetSdrClient>> _mockLogger;
    private readonly NetSdrClient _client;

    public NetSdrClientTests()
    {
        _mockNetworkClient = new Mock<INetworkClient>();
        _mockLogger = new Mock<ILogger<NetSdrClient>>();
        _client = new NetSdrClient(_mockNetworkClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ConnectAsync_ShouldConnect_WhenValidParameters()
    {
        // Arrange
        const string host = "localhost";
        const int port = 50000;

        // Act
        await _client.ConnectAsync(host, port);

        // Assert
        _mockNetworkClient.Verify(x => x.ConnectAsync(host, port), Times.Once);
        VerifyLog(LogLevel.Information, $"Connected to {host}:{port}");
    }

    [Fact]
    public async Task ConnectAsync_ShouldThrow_WhenConnectionFails()
    {
        // Arrange
        _mockNetworkClient.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Connection failed"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _client.ConnectAsync("localhost"));

        VerifyLog(LogLevel.Error, "Failed to connect to localhost:50000");
    }

    [Fact]
    public async Task ConnectAsync_InvalidHost_ThrowsException()
    {
        // Arrange
        var invalidHost = "invalid-host";
        var port = 50000;

        _mockNetworkClient.Setup(n => n.ConnectAsync(invalidHost, port)).ThrowsAsync(new SocketException());

        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(() => _client.ConnectAsync(invalidHost, port));

        VerifyLog(LogLevel.Error, "Failed to connect");
    }

    [Fact]
    public async Task ConnectAsync_InvalidPort_ThrowsException()
    {
        // Arrange
        var host = "127.0.0.1";
        var invalidPort = -1;

        _mockNetworkClient.Setup(n => n.ConnectAsync(host, invalidPort)).ThrowsAsync(new ArgumentOutOfRangeException());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _client.ConnectAsync(host, invalidPort));

        VerifyLog(LogLevel.Error, "Failed to connect");
    }

    [Fact]
    public async Task DisconnectAsync_ShouldDisconnect_WhenConnected()
    {
        // Act
        await _client.DisconnectAsync();

        // Assert
        _mockNetworkClient.Verify(x => x.DisconnectAsync(), Times.Once);
        VerifyLog(LogLevel.Information, "Disconnected from device");
    }


    [Fact]
    public async Task DisconnectAsync_ThrowsException()
    {
        // Arrange
        _mockNetworkClient.Setup(n => n.DisconnectAsync()).ThrowsAsync(new InvalidOperationException());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _client.DisconnectAsync());

        VerifyLog(LogLevel.Error, "Error");
    }

    [Fact]
    public async Task ConnectAsync_Failure_LogsAndThrows()
    {
        // Arrange
        var host = "127.0.0.1";
        var port = 50000;

        _mockNetworkClient.Setup(n => n.ConnectAsync(host, port)).ThrowsAsync(new SocketException());

        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(() => _client.ConnectAsync(host, port));

        VerifyLog(LogLevel.Error, "Failed to connect");
    }

    [Fact]
    public async Task StartIqTransmissionAsync_ShouldSendCorrectCommand()
    {
        // Arrange
        SetupSuccessfulCommand();

        // Act
        await _client.StartIqTransmissionAsync();

        // Assert
        _mockNetworkClient.Verify(x => x.SendAsync("set RX On"), Times.Once);
    }

    [Fact]
    public async Task StopIqTransmissionAsync_ShouldSendCorrectCommand()
    {
        // Arrange
        SetupSuccessfulCommand();

        // Act
        await _client.StopIqTransmissionAsync();

        // Assert
        _mockNetworkClient.Verify(x => x.SendAsync("set RX Off"), Times.Once);
    }

    [Fact]
    public async Task SetFrequencyAsync_ShouldSendCorrectCommand()
    {
        // Arrange
        SetupSuccessfulCommand();
        const int frequency = 100_000_000;

        // Act
        await _client.SetFrequencyAsync(frequency);

        // Assert
        _mockNetworkClient.Verify(x => x.SendAsync($"set RXFrequency {frequency}"), Times.Once);
    }

    [Fact]
    public async Task SetFrequencyAsync_ThrowsOnNAK()
    {
        // Arrange
        _mockNetworkClient.Setup(n => n.IsConnected).Returns(true);
        _mockNetworkClient.Setup(n => n.SendAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockNetworkClient.Setup(n => n.ReceiveAsync()).ReturnsAsync("NAK Frequency out of range");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _client.SetFrequencyAsync(123456789));
        Assert.Contains("NAK", ex.Message);
    }

    [Fact]
    public async Task SetFrequencyAsync_ThrowsIfNotConnected()
    {
        // Arrange
        _mockNetworkClient.Setup(n => n.IsConnected).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _client.SetFrequencyAsync(100000000));
    }

    [Fact]
    public async Task SendCommand_ShouldThrow_WhenReceivesNAK()
    {
        // Arrange
        _mockNetworkClient.Setup(x => x.IsConnected).Returns(true);
        _mockNetworkClient.Setup(x => x.ReceiveAsync())
            .ReturnsAsync("NAK");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _client.SetFrequencyAsync(100_000_000));
    }

    [Fact]
    public async Task SendCommand_ShouldThrow_WhenNotConnected()
    {
        // Arrange
        _mockNetworkClient.Setup(x => x.IsConnected).Returns(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _client.SetFrequencyAsync(100_000_000));
        Assert.Equal("Not connected", ex.Message);
    }

    private void SetupSuccessfulCommand()
    {
        _mockNetworkClient.Setup(x => x.IsConnected).Returns(true);
        _mockNetworkClient.Setup(x => x.ReceiveAsync())
            .ReturnsAsync("ACK");
    }

    private void VerifyLog(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
