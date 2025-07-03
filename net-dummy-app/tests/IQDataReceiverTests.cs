using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using NetDummyApp;

namespace NetDummyApp.Tests;

/// <summary>
/// Interface for a UDP client wrapper to allow mocking in tests.
/// </summary>
public interface IUdpClientWrapper
{
    Task<UdpReceiveResult> ReceiveAsync();
    int Available { get; }
}

/// <summary>
/// Represents the result of a UDP receive operation.
/// </summary>
public class UdpReceiveResult
{
    public byte[] Buffer { get; set; } = [];
    public IPEndPoint RemoteEndPoint { get; set; } = new IPEndPoint(IPAddress.Loopback, 60000);
}

/// <summary>
/// Base class for receiving I/Q data over UDP.
/// </summary>
public abstract class IQDataReceiverBase : IQDataReceiver
{
    protected IQDataReceiverBase(string address, int port, ILogger? logger = null) : base(address, port, logger) { }
    protected abstract Task<UdpReceiveResult> ReceivePacketAsync();
    protected abstract int GetAvailable();
}

/// <summary>
/// Testable version of IQDataReceiver for unit testing.
/// This class allows injecting a mock UDP client to simulate receiving data.
/// </summary>
public class IQDataReceiverTestable(IUdpClientWrapper udp, ILogger? logger = null) : IQDataReceiverBase("127.0.0.1", 60000, logger)
{
    public IUdpClientWrapper UdpClient { get; } = udp;

    protected override async Task<UdpReceiveResult> ReceivePacketAsync()
    {
        return await UdpClient.ReceiveAsync();
    }

    protected override int GetAvailable() => UdpClient.Available;
}

/// <summary>
/// Unit tests for the IQDataReceiver class.
/// </summary>
public class IQDataReceiverTests
{
    private readonly Mock<IUdpClientWrapper> _mockUdpClient;
    private readonly Mock<ILogger> _mockLogger;

    public IQDataReceiverTests()
    {
        _mockUdpClient = new Mock<IUdpClientWrapper>();
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public async Task StartReceivingAsync_ReceivesAndWritesData()
    {
        // Arrange
        var packets = new Queue<byte[]>(
        [
            [1, 2, 3, 4],
            [5, 6, 7, 8]
        ]);

        _mockUdpClient.SetupSequence(u => u.Available).Returns(1).Returns(1).Returns(0);

        _mockUdpClient.Setup(u => u.ReceiveAsync()).ReturnsAsync(() => new UdpReceiveResult
        {
            Buffer = packets.Dequeue(),
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 60000)
        });

        var filePath = Path.GetTempFileName();
        var receiver = new IQDataReceiverTestable(_mockUdpClient.Object, _mockLogger.Object);

        // Act
        await receiver.StartReceivingAsync(filePath, TimeSpan.FromMilliseconds(100));

        // Assert
        var bytes = await File.ReadAllBytesAsync(filePath);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], bytes);

        File.Delete(filePath);
    }

    [Fact]
        public async Task StartReceivingAsync_HandlesEmptyPackets()
        {
            // Arrange
            var packets = new Queue<byte[]>([[]]);

            _mockUdpClient.SetupSequence(u => u.Available).Returns(1).Returns(0);
            _mockUdpClient.Setup(u => u.ReceiveAsync()).ReturnsAsync(() => new UdpReceiveResult
            {
                Buffer = packets.Dequeue(),
                RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 60000)
            });

            var filePath = Path.GetTempFileName();
            var receiver = new IQDataReceiverTestable(_mockUdpClient.Object, _mockLogger.Object);

            // Act
            await receiver.StartReceivingAsync(filePath, TimeSpan.FromMilliseconds(100));

            // Assert
            var bytes = await File.ReadAllBytesAsync(filePath);
            Assert.Empty(bytes);

            File.Delete(filePath);
        }

        [Fact]
        public async Task StartReceivingAsync_HandlesReceptionException()
        {
            // Arrange
            _mockUdpClient.Setup(u => u.ReceiveAsync()).ThrowsAsync(new InvalidOperationException("Receive failed"));

            var filePath = Path.GetTempFileName();
            var receiver = new IQDataReceiverTestable(_mockUdpClient.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => receiver.StartReceivingAsync(filePath, TimeSpan.FromMilliseconds(100)));

            File.Delete(filePath);
        }

        [Fact]
        public async Task StartReceivingAsync_RespectsTimeout()
        {
            // Arrange
            _mockUdpClient.Setup(u => u.Available).Returns(0);

            var filePath = Path.GetTempFileName();
            var receiver = new IQDataReceiverTestable(_mockUdpClient.Object, _mockLogger.Object);

            // Act
            var startTime = DateTime.UtcNow;
            await receiver.StartReceivingAsync(filePath, TimeSpan.FromMilliseconds(100));
            var elapsedTime = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(elapsedTime >= TimeSpan.FromMilliseconds(100));

            File.Delete(filePath);
        }

        [Fact]
        public async Task StartReceivingAsync_CleansUpResourcesProperly()
        {
            // Arrange
            var packets = new Queue<byte[]>([[1, 2, 3, 4]]);

            _mockUdpClient.SetupSequence(u => u.Available).Returns(1).Returns(0);
            _mockUdpClient.Setup(u => u.ReceiveAsync()).ReturnsAsync(() => new UdpReceiveResult
            {
                Buffer = packets.Dequeue(),
                RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 60000)
            });

            var filePath = Path.GetTempFileName();
            var receiver = new IQDataReceiverTestable(_mockUdpClient.Object, _mockLogger.Object);

            // Act
            await receiver.StartReceivingAsync(filePath, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(File.Exists(filePath));
            File.Delete(filePath);
            Assert.False(File.Exists(filePath));
        }
}