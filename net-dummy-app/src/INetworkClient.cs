namespace NetDummyApp;

public interface INetworkClient: IDisposable
{
    Task ConnectAsync(string host, int port);
    Task DisconnectAsync();
    Task SendAsync(string command);
    Task<string> ReceiveAsync();
    Stream GetStream();
    bool IsConnected { get; }
}