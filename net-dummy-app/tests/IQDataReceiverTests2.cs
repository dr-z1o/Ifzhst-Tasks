using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NetDummyApp.Tests
{
    public class IQDataReceiverTests2 : IDisposable
    {
        private readonly Mock<IUdpClient> _mockUdpClient;
        private readonly Mock<ILogger<IQDataReceiver>> _mockLogger;
        private readonly string _tempFilePath;

        public IQDataReceiverTests2()
        {
            _mockUdpClient = new Mock<IUdpClient>();
            _mockLogger = new Mock<ILogger<IQDataReceiver>>();
            _tempFilePath = Path.GetTempFileName();
        }

        [Fact]
        public async Task StartReceivingAsync_WritesReceivedDataToFile()
        {
            // Arrange
            var testData = new byte[] { 1, 2, 3, 4 };
            _mockUdpClient.SetupSequence(x => x.Available)
                .Returns(testData.Length)
                .Returns(0);

            _mockUdpClient.Setup(x => x.ReceiveAsync())
                .ReturnsAsync(new UdpReceiveResult(
                    testData,
                    new IPEndPoint(IPAddress.Loopback, 60000)));

            var receiver = new IQDataReceiver(_mockUdpClient.Object, _mockLogger.Object);

            // Act
            await receiver.StartReceivingAsync(_tempFilePath, TimeSpan.FromMilliseconds(100));

            // Assert
            var fileBytes = await File.ReadAllBytesAsync(_tempFilePath);
            Assert.Equal(testData, fileBytes);
        }

        [Fact]
        public async Task StartReceivingAsync_HandlesMultiplePackets()
        {
            // Arrange
            var packet1 = new byte[] { 1, 2, 3, 4 };
            var packet2 = new byte[] { 5, 6, 7, 8 };
            
            var receiveQueue = new Queue<UdpReceiveResult>(new[]
            {
                new UdpReceiveResult(packet1, new IPEndPoint(IPAddress.Loopback, 60000)),
                new UdpReceiveResult(packet2, new IPEndPoint(IPAddress.Loopback, 60000))
            });

            _mockUdpClient.SetupSequence(x => x.Available)
                .Returns(packet1.Length)
                .Returns(packet2.Length)
                .Returns(0);

            _mockUdpClient.Setup(x => x.ReceiveAsync())
                .Returns(() => Task.FromResult(receiveQueue.Dequeue()));

            var receiver = new IQDataReceiver(_mockUdpClient.Object, _mockLogger.Object);

            // Act
            await receiver.StartReceivingAsync(_tempFilePath, TimeSpan.FromMilliseconds(200));

            // Assert
            var fileBytes = await File.ReadAllBytesAsync(_tempFilePath);
            Assert.Equal(packet1.Concat(packet2).ToArray(), fileBytes);
        }

        [Fact]
        public async Task StartReceivingAsync_LogsErrorAndThrows()
        {
            // Arrange
            var testException = new SocketException();
            _mockUdpClient.Setup(x => x.Available).Returns(1);
            _mockUdpClient.Setup(x => x.ReceiveAsync())
                .ThrowsAsync(testException);

            var receiver = new IQDataReceiver(_mockUdpClient.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<SocketException>(() => 
                receiver.StartReceivingAsync(_tempFilePath, TimeSpan.FromMilliseconds(100)));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    testException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [Fact]
        public void Constructor_InitializesWithAddressAndPort()
        {
            // Arrange & Act
            var receiver = new IQDataReceiver("127.0.0.1", 60000, _mockLogger.Object);

            // Assert
            Assert.NotNull(receiver);
        }

        [Fact]
        public void Dispose_CleanupResources()
        {
            // Arrange
            var receiver = new IQDataReceiver(_mockUdpClient.Object, _mockLogger.Object);

            // Act
            receiver.Dispose();
            receiver.Dispose(); // Second dispose should not throw

            // Assert
            _mockUdpClient.Verify(x => x.Dispose(), Times.Once);
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }
    }
}