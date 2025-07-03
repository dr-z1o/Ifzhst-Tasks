using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using NetDummyApp;

public class NetSdrClientTests
{
    [Fact]
    public async Task SetFrequencyAsync_SendsCorrectCommand()
    {
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        var sentData = new MemoryStream();

        mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), default))
            .Returns((byte[] buffer, int offset, int count, System.Threading.CancellationToken token) =>
            {
                sentData.Write(buffer, offset, count);
                return Task.CompletedTask;
            });

        mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), default))
            .ReturnsAsync((byte[] buffer, int offset, int count, System.Threading.CancellationToken token) =>
            {
                var ack = Encoding.ASCII.GetBytes("ACK\n");
                ack.CopyTo(buffer, offset);
                return ack.Length;
            });

        var client = new TestableNetSdrClient(mockStream.Object);
        await client.SetFrequencyAsync(123456789);

        var result = Encoding.ASCII.GetString(sentData.ToArray());
        Assert.Equal("set RXFrequency 123456789\n", result);
    }

    [Fact]
    public async Task StartIqTransmissionAsync_SendsCorrectCommand()
    {
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        var sentData = new MemoryStream();

        mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), default))
            .Returns((byte[] buffer, int offset, int count, System.Threading.CancellationToken token) =>
            {
                sentData.Write(buffer, offset, count);
                return Task.CompletedTask;
            });

        mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), default))
            .ReturnsAsync((byte[] buffer, int offset, int count, System.Threading.CancellationToken token) =>
            {
                var ack = Encoding.ASCII.GetBytes("ACK\n");
                ack.CopyTo(buffer, offset);
                return ack.Length;
            });

        var client = new TestableNetSdrClient(mockStream.Object);
        await client.StartIqTransmissionAsync();

        var result = Encoding.ASCII.GetString(sentData.ToArray());
        Assert.Equal("set RX On\n", result);
    }

    private class TestableNetSdrClient(NetworkStream mockStream) : NetSdrClient(new Mock<ILogger>().Object)
    {
        private readonly NetworkStream _mockStream = mockStream;

        protected override NetworkStream GetStream() => _mockStream;
    }
}