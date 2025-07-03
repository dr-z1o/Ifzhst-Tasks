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

public interface IUdpClientWrapper
{
    Task<UdpReceiveResult> ReceiveAsync();
    int Available { get; }
}

public class UdpReceiveResult
{
    public byte[] Buffer { get; set; } = Array.Empty<byte>();
    public IPEndPoint RemoteEndPoint { get; set; } = new IPEndPoint(IPAddress.Loopback, 60000);
}

public abstract class IQDataReceiverBase : IQDataReceiver
{
    protected IQDataReceiverBase(string address, int port, ILogger logger = null) : base(address, port, logger) { }
    protected abstract Task<UdpReceiveResult> ReceivePacketAsync();
    protected abstract int GetAvailable();
}

public class IQDataReceiverTestable : IQDataReceiverBase
{
    public IQDataReceiverTestable(IUdpClientWrapper udp, ILogger logger = null)
        : base("127.0.0.1", 60000, logger)
    {
        UdpClient = udp;
    }

    public IUdpClientWrapper UdpClient { get; }

    protected override async Task<UdpReceiveResult> ReceivePacketAsync()
    {
        return await UdpClient.ReceiveAsync();
    }

    protected override int GetAvailable() => UdpClient.Available;
}

public class IQDataReceiverTests
{
    [Fact]
    public async Task StartReceivingAsync_ReceivesAndWritesData()
    {
        var mockUdp = new Mock<IUdpClientWrapper>();
        var packets = new Queue<byte[]>(new[]
        {
            new byte[] {1, 2, 3, 4},
            new byte[] {5, 6, 7, 8}
        });

        mockUdp.SetupSequence(u => u.Available)
            .Returns(1).Returns(1).Returns(0);

        mockUdp.Setup(u => u.ReceiveAsync()).ReturnsAsync(() => new UdpReceiveResult
        {
            Buffer = packets.Dequeue(),
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 60000)
        });

        var filePath = Path.GetTempFileName();
        var logger = Mock.Of<ILogger>();
        var receiver = new IQDataReceiverTestable(mockUdp.Object, logger);

        await receiver.StartReceivingAsync(filePath, TimeSpan.FromMilliseconds(100));

        var bytes = await File.ReadAllBytesAsync(filePath);
        Assert.Equal(new byte[] {1, 2, 3, 4, 5, 6, 7, 8}, bytes);

        File.Delete(filePath);
    }
}