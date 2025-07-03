using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using NetDummyApp;

namespace NetDummyApp.Tests;
// This file contains unit tests for the NetSdrClient class.
public class NetSdrClientTests
{
    private readonly Mock<ILogger<NetSdrClient>> _mockLogger;
    private readonly Mock<INetworkClient> _mockNetwork;
    private readonly NetSdrClient _client;

    public NetSdrClientTests()
    {
        _mockLogger = new Mock<ILogger<NetSdrClient>>();
        _mockNetwork = new Mock<INetworkClient>();

        _client = new NetSdrClient(_mockNetwork.Object, _mockLogger.Object);

    }

    [Fact]
    public async Task ConnectAsync_InvalidHost_ThrowsException()
    {
        // Arrange
        var invalidHost = "invalid-host";
        var port = 50000;

        _mockNetwork.Setup(n => n.ConnectAsync(invalidHost, port)).ThrowsAsync(new SocketException());

        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(() => _client.ConnectAsync(invalidHost, port));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to connect")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task ConnectAsync_InvalidPort_ThrowsException()
    {
        // Arrange
        var host = "127.0.0.1";
        var invalidPort = -1;

        _mockNetwork.Setup(n => n.ConnectAsync(host, invalidPort)).ThrowsAsync(new ArgumentOutOfRangeException());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _client.ConnectAsync(host, invalidPort));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to connect")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task ConnectAsync_Success()
    {
        // Arrange
        var host = "127.0.0.1";
        var port = 50000;

        _mockNetwork.Setup(n => n.ConnectAsync(host, port)).Returns(Task.CompletedTask);

        // Act
        await _client.ConnectAsync(host, port);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Connected to")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task ConnectAsync_Failure_LogsAndThrows()
    {
        // Arrange
        var host = "127.0.0.1";
        var port = 50000;

        _mockNetwork.Setup(n => n.ConnectAsync(host, port)).ThrowsAsync(new SocketException());

        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(() => _client.ConnectAsync(host, port));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to connect")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task DisconnectAsync_Success()
    {
        // Arrange
        _mockNetwork.Setup(n => n.DisconnectAsync()).Returns(Task.CompletedTask);

        // Act
        await _client.DisconnectAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Disconnected from device")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once
        );
        _mockNetwork.Verify(n => n.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task DisconnectAsync_ThrowsException()
    {
        // Arrange
        _mockNetwork.Setup(n => n.DisconnectAsync()).ThrowsAsync(new InvalidOperationException());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _client.DisconnectAsync());

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task StartIqTransmissionAsync_SendsCommand()
    {
        // Arrange
        var command = "set RX On";
        _mockNetwork.Setup(n => n.IsConnected).Returns(true);
        _mockNetwork.Setup(n => n.SendAsync(command)).Returns(Task.CompletedTask);
        _mockNetwork.Setup(n => n.ReceiveAsync()).ReturnsAsync("ACK");

        // Act
        await _client.StartIqTransmissionAsync();

        // Assert
        _mockNetwork.Verify(n => n.SendAsync(command), Times.Once);
        _mockNetwork.Verify(n => n.ReceiveAsync(), Times.Once);
    }

    [Fact]
    public async Task StopIqTransmissionAsync_SendsCommand()
    {
        // Arrange
        var command = "set RX Off";
        _mockNetwork.Setup(n => n.IsConnected).Returns(true);
        _mockNetwork.Setup(n => n.SendAsync(command)).Returns(Task.CompletedTask);
        _mockNetwork.Setup(n => n.ReceiveAsync()).ReturnsAsync("ACK");

        // Act
        await _client.StopIqTransmissionAsync();

        // Assert
        _mockNetwork.Verify(n => n.SendAsync(command), Times.Once);
        _mockNetwork.Verify(n => n.ReceiveAsync(), Times.Once);
    }

    [Fact]
    public async Task SetFrequencyAsync_SendsCommand()
    {
        // Arrange
        _mockNetwork.Setup(n => n.IsConnected).Returns(true);
        _mockNetwork.Setup(n => n.SendAsync("set RXFrequency 123456789")).Returns(Task.CompletedTask);
        _mockNetwork.Setup(n => n.ReceiveAsync()).ReturnsAsync("ACK");

        // Act
        await _client.SetFrequencyAsync(123456789);

        // Assert
        _mockNetwork.Verify(n => n.SendAsync("set RXFrequency 123456789"), Times.Once);
        _mockNetwork.Verify(n => n.ReceiveAsync(), Times.Once);
    }

    [Fact]
    public async Task SetFrequencyAsync_ThrowsOnNAK()
    {
        // Arrange
        _mockNetwork.Setup(n => n.IsConnected).Returns(true);
        _mockNetwork.Setup(n => n.SendAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockNetwork.Setup(n => n.ReceiveAsync()).ReturnsAsync("NAK Frequency out of range");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _client.SetFrequencyAsync(123456789));
        Assert.Contains("NAK", ex.Message);
    }

    [Fact]
    public async Task SetFrequencyAsync_ThrowsIfNotConnected()
    {
        // Arrange
        _mockNetwork.Setup(n => n.IsConnected).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _client.SetFrequencyAsync(100000000));
    }
}