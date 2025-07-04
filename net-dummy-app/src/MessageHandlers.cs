using Microsoft.Extensions.Logging;

namespace NetDummyApp;

/// <summary>
/// Interface for handling messages.
/// This interface defines methods for handling ACK, NAK, and unsolicited messages.
/// It allows for different implementations to handle these messages as needed.
/// </summary>
/// <remarks>
/// ACK (Acknowledgment) messages indicate successful processing of a command.
/// NAK (Negative Acknowledgment) messages indicate failure to process a command, often with a reason.
/// Unsolicited messages are messages that are sent without a specific request, often used for notifications.
/// </remarks>
public interface IMessageHandler
{
    void HandleAck(string message);
    void HandleNak(string message);
    void HandleUnsolicited(string message);
}

/// <summary>
/// Interface for handling unsolicited messages.
/// </summary>
public interface IUnsolicitedMessageHandler
{
    void OnUnsolicitedMessage(string message);
}

/// <summary>
/// Default implementation of IMessageHandler.
/// This class handles ACK and NAK messages, and delegates unsolicited messages to an IUnsolicitedMessageHandler.
/// If no unsolicited handler is provided, it uses a DefaultUnsolicitedMessageHandler that logs the message.
/// </summary>
public class DefaultMessageHandler(IUnsolicitedMessageHandler? unsolicitedHandler = null, ILogger? logger = null) : IMessageHandler
{
    private readonly ILogger? _logger = logger;
    private readonly IUnsolicitedMessageHandler? _unsolicitedHandler = unsolicitedHandler ?? new DefaultUnsolicitedMessageHandler(logger);

    public void HandleAck(string message)
    {
        _logger?.LogInformation("ACK received: {Message}", message.Trim());
    }

    public void HandleNak(string message)
    {
        string reason = message.Length > 3 ? message.Substring(3).Trim() : "Unknown reason";
        _logger?.LogError("NAK received: {Reason}", reason);
        throw new InvalidOperationException($"Received NAK: {reason}");
    }

    public void HandleUnsolicited(string message)
    {
        _unsolicitedHandler?.OnUnsolicitedMessage(message);
    }
}

/// <summary>
/// Default implementation of IUnsolicitedMessageHandler.
/// This class logs unsolicited messages using the provided ILogger.
/// </summary>
public class DefaultUnsolicitedMessageHandler(ILogger? logger) : IUnsolicitedMessageHandler
{
    private readonly ILogger? _logger = logger;

    public void OnUnsolicitedMessage(string message)
    {
        _logger?.LogWarning("Unsolicited message received: {Message}", message.Trim());
    }
}