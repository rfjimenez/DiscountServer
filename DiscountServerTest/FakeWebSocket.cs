using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// A minimal fake implementation of WebSocket for unit testing purposes.
/// Allows simulation of receiving messages from a client and capturing messages sent by the server.
/// </summary>
public class FakeWebSocket : WebSocket
{
    // Queue of messages to simulate client input
    private readonly Queue<string> _messages;

    /// <summary>
    /// Stores all text messages sent by the server for assertion in tests.
    /// </summary>
    public List<string> SentTextMessages { get; } = new();

    /// <summary>
    /// Stores all binary messages sent by the server for assertion in tests.
    /// </summary>
    public List<byte[]> SentBinaryMessages { get; } = new();

    /// <summary>
    /// Initializes the fake WebSocket with a sequence of messages to be received.
    /// </summary>
    /// <param name="messages">Messages to simulate as incoming from the client.</param>
    public FakeWebSocket(params string[] messages)
    {
        _messages = new Queue<string>(messages);
    }

    // Properties to mimic the real WebSocket state
    public override WebSocketCloseStatus? CloseStatus => null;
    public override string CloseStatusDescription => null;
    public override WebSocketState State => _messages.Count > 0 ? WebSocketState.Open : WebSocketState.Closed;
    public override string SubProtocol => null;

    public override void Abort() { }
    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;
    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;

    public override void Dispose() { }

    /// <summary>
    /// Simulates receiving a message from the client.
    /// Dequeues the next message and copies it into the provided buffer.
    /// Returns a Close result if no messages remain.
    /// </summary>
    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        if (_messages.Count == 0)
            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);

        var msg = _messages.Dequeue();
        var bytes = Encoding.UTF8.GetBytes(msg);
        Array.Copy(bytes, buffer.Array, bytes.Length);
        return new WebSocketReceiveResult(bytes.Length, WebSocketMessageType.Text, true);
    }

    /// <summary>
    /// Captures messages sent by the server for later assertion.
    /// Stores text and binary messages in separate lists.
    /// </summary>
    public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        if (messageType == WebSocketMessageType.Text)
            SentTextMessages.Add(Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count));
        else if (messageType == WebSocketMessageType.Binary)
            SentBinaryMessages.Add(buffer.ToArray());
    }
}