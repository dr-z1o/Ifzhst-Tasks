using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Net.Sockets;

namespace NetDummyApp.Tests;

/// <summary>
/// Unit tests for the IQDataReceiver class.
/// </summary>
public class IQDataReceiverTests : IDisposable
{
    private readonly Mock<IUdpClient> _mockUdpClient;
    private readonly Mock<ILogger<IQDataReceiver>> _mockLogger;
    private readonly string _tempFilePath;

    public IQDataReceiverTests()
    {
        _mockUdpClient = new Mock<IUdpClient>();
        _mockLogger = new Mock<ILogger<IQDataReceiver>>();
        _tempFilePath = Path.GetTempFileName();
    }

    [Fact]
    public async Task StartReceivingAsync_ShouldReceiveData()
    {
        // Arrange
        var mockUdpClient = new Mock<IUdpClient>();
        mockUdpClient.Setup(u => u.Available).Returns(100);
        mockUdpClient.Setup(u => u.ReceiveAsync())
            .Returns(Task.FromResult(new UdpReceiveResult(
                    [1, 2, 3],
                    new IPEndPoint(IPAddress.Any, 0))));

        var receiver = new IQDataReceiver(mockUdpClient.Object, _mockLogger.Object);

        // Act
        await receiver.StartReceivingAsync("test.bin", TimeSpan.FromMilliseconds(100));

        // Assert
        mockUdpClient.Verify(x => x.ReceiveAsync(), Times.AtLeastOnce());
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
        var firstPacket = new byte[] { 1, 2, 3, 4 };
        var secondPacket = new byte[] { 5, 6, 7, 8 };

        // Setup Available to allow both packets to be read
        _mockUdpClient.SetupSequence(u => u.Available)
            .Returns(firstPacket.Length)  // First packet
            .Returns(secondPacket.Length) // Second packet
            .Returns(0);                  // End condition

        var receiveSequence = new Queue<UdpReceiveResult>(
            [
                new UdpReceiveResult(firstPacket, new IPEndPoint(IPAddress.Loopback, 60000)),
                new UdpReceiveResult(secondPacket, new IPEndPoint(IPAddress.Loopback, 60000))
            ]);

        _mockUdpClient.Setup(u => u.ReceiveAsync())
            .Returns(() => Task.FromResult(receiveSequence.Dequeue()));

        var receiver = new IQDataReceiver(_mockUdpClient.Object, _mockLogger.Object);

        // Act
        await receiver.StartReceivingAsync(_tempFilePath, TimeSpan.FromMilliseconds(200));

        // Assert
        var bytes = await File.ReadAllBytesAsync(_tempFilePath);
        Assert.Equal(firstPacket.Concat(secondPacket).ToArray(), bytes);
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
    public async Task StartReceivingAsync_HandlesEmptyPackets()
    {
        // Arrange
        var packets = new Queue<byte[]>([[]]);

        _mockUdpClient.SetupSequence(u => u.Available).Returns(1).Returns(0);
        _mockUdpClient.Setup(u => u.ReceiveAsync()).ReturnsAsync(() => new UdpReceiveResult(
                        packets.Dequeue(),
                        new IPEndPoint(IPAddress.Loopback, 60000)));

        var receiver = new IQDataReceiver(_mockUdpClient.Object, _mockLogger.Object);

        // Act
        await receiver.StartReceivingAsync(_tempFilePath, TimeSpan.FromMilliseconds(100));

        // Assert
        var bytes = await File.ReadAllBytesAsync(_tempFilePath);
        Assert.Empty(bytes);
    }

    [Fact]
    public async Task StartReceivingAsync_HandlesReceptionException()
    {
        // Arrange
        _mockUdpClient.Setup(u => u.Available).Returns(1);
        _mockUdpClient.Setup(u => u.ReceiveAsync())
            .ThrowsAsync(new InvalidOperationException("Receive failed"));

        var receiver = new IQDataReceiver(_mockUdpClient.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => receiver.StartReceivingAsync(_tempFilePath, TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public async Task StartReceivingAsync_RespectsTimeout()
    {
        // Arrange
        _mockUdpClient.Setup(u => u.Available).Returns(0);

        var receiver = new IQDataReceiver(_mockUdpClient.Object, _mockLogger.Object);

        // Act
        var startTime = DateTime.UtcNow;
        await receiver.StartReceivingAsync(_tempFilePath, TimeSpan.FromMilliseconds(100));
        var elapsedTime = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(elapsedTime >= TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task StartReceivingAsync_CleansUpResourcesProperly()
    {
        // Arrange
        var packets = new Queue<byte[]>([[1, 2, 3, 4]]);

        _mockUdpClient.SetupSequence(u => u.Available).Returns(1).Returns(0);
        _mockUdpClient.Setup(u => u.ReceiveAsync()).ReturnsAsync(() => new UdpReceiveResult(
            packets.Dequeue(),
            new IPEndPoint(IPAddress.Loopback, 60000)));

        var filePath = Path.GetTempFileName();
        var receiver = new IQDataReceiver(_mockUdpClient.Object, _mockLogger.Object);

        // Act
        await receiver.StartReceivingAsync(filePath, TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.True(File.Exists(filePath));
        File.Delete(filePath);
        Assert.False(File.Exists(filePath));
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