// filepath: /Users/dr_zlo/projects/Infozahyst Test Task/net-dummy-app/src/NetSdrClientTest.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NetDummyApp.Tests
{
    public class NetSdrClientTest
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<NetworkStream> _mockStream;
        private readonly Mock<TcpClient> _mockTcpClient;
        private readonly NetSdrClient _client;

        public NetSdrClientTest()
        {
            _mockLogger = new Mock<ILogger>();
            _mockStream = new Mock<NetworkStream>();
            _mockTcpClient = new Mock<TcpClient>();

            _client = new NetSdrClient(_mockLogger.Object)
            {
                // Inject mock TcpClient and NetworkStream
                _tcpClient = _mockTcpClient.Object,
                _stream = _mockStream.Object
            };
        }

        [Fact]
        public async Task ConnectAsync_Success()
        {
            // Arrange
            _mockTcpClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _mockTcpClient.Setup(c => c.GetStream()).Returns(_mockStream.Object);

            // Act
            await _client.ConnectAsync("127.0.0.1", 50000);

            // Assert
            _mockLogger.Verify(l => l.LogInformation("Connected to {host}:{port}", "127.0.0.1", 50000), Times.Once);
        }

        [Fact]
        public async Task ConnectAsync_Failure()
        {
            // Arrange
            _mockTcpClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new SocketException());

            // Act & Assert
            await Assert.ThrowsAsync<SocketException>(() => _client.ConnectAsync("127.0.0.1", 50000));
            _mockLogger.Verify(l => l.LogError(It.IsAny<Exception>(), "Failed to connect to {host}:{port}", "127.0.0.1", 50000), Times.Once);
        }

        [Fact]
        public async Task DisconnectAsync_Success()
        {
            // Arrange
            _mockStream.Setup(s => s.FlushAsync()).Returns(Task.CompletedTask);

            // Act
            await _client.DisconnectAsync();

            // Assert
            _mockLogger.Verify(l => l.LogInformation("Disconnected from device"), Times.Once);
        }

        [Fact]
        public async Task DisconnectAsync_Failure()
        {
            // Arrange
            _mockStream.Setup(s => s.FlushAsync()).ThrowsAsync(new IOException());

            // Act & Assert
            await Assert.ThrowsAsync<IOException>(() => _client.DisconnectAsync());
            _mockLogger.Verify(l => l.LogError(It.IsAny<Exception>(), "Error during disconnect"), Times.Once);
        }

        [Fact]
        public async Task StartIqTransmissionAsync_SendsCommand()
        {
            // Arrange
            _mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .ReturnsAsync(Encoding.ASCII.GetBytes("ACK").Length);

            // Act
            await _client.StartIqTransmissionAsync();

            // Assert
            _mockLogger.Verify(l => l.LogInformation("Sending command: {command}", "set RX On"), Times.Once);
        }

        [Fact]
        public async Task StopIqTransmissionAsync_SendsCommand()
        {
            // Arrange
            _mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .ReturnsAsync(Encoding.ASCII.GetBytes("ACK").Length);

            // Act
            await _client.StopIqTransmissionAsync();

            // Assert
            _mockLogger.Verify(l => l.LogInformation("Sending command: {command}", "set RX Off"), Times.Once);
        }

        [Fact]
        public async Task SetFrequencyAsync_SendsCommand()
        {
            // Arrange
            _mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .ReturnsAsync(Encoding.ASCII.GetBytes("ACK").Length);

            // Act
            await _client.SetFrequencyAsync(123456);

            // Assert
            _mockLogger.Verify(l => l.LogInformation("Sending command: {command}", "set RXFrequency 123456"), Times.Once);
        }
    }
}