using DiscountServer.Models;
using DiscountServer.Services;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace DiscountServer.Handlers
{
    /// <summary>
    /// Handles WebSocket connections for discount code generation and usage.
    /// </summary>
    public class WebSocketHandler
    {
        // Command identifiers for WebSocket messages
        private const string GENERATE = "GENERATE";
        private const string USE = "USE";
        private readonly DiscountService _discountService;

        /// <summary>
        /// Initializes a new instance of the WebSocketHandler class.
        /// </summary>
        /// <param name="discountService">Service for discount code operations.</param>
        public WebSocketHandler(DiscountService discountService)
        {
            _discountService = discountService;
        }

        /// <summary>
        /// Processes incoming WebSocket messages and dispatches requests.
        /// </summary>
        /// <param name="webSocket">Active WebSocket connection.</param>
        public async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024];

            // Main loop: receive and handle messages while the connection is open
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                // Handle client-initiated close
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }

                // Decode received message
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                // Validate message content
                if (string.IsNullOrWhiteSpace(receivedMessage))
                {
                    await SendResponseAsync(webSocket, DiscountCodeResult.InvalidRequest);
                    continue;
                }

                // Parse command and arguments
                var parts = receivedMessage.Split('|');
                switch (parts[0])
                {
                    case GENERATE:
                        await HandleGenerateRequest(parts, webSocket);
                        break;
                    case USE:
                        await HandleUseRequest(parts, webSocket);
                        break;
                    default:
                        await SendResponseAsync(webSocket, DiscountCodeResult.InvalidRequest);
                        break;
                }
            }
        }

        /// <summary>
        /// Handles a GENERATE command to create new discount codes.
        /// </summary>
        /// <param name="parts">Command arguments: [GENERATE, count, length]</param>
        /// <param name="webSocket">WebSocket connection to respond to.</param>
        private async Task HandleGenerateRequest(string[] parts, WebSocket webSocket)
        {
            // Validate argument count and types
            if (parts.Length == 3 &&
                ushort.TryParse(parts[1], out ushort count) &&
                byte.TryParse(parts[2], out byte length))
            {
                // Generate codes using the service
                var codes = _discountService.GenerateCodes(count, length);
                if (codes.Count == 0)
                {
                    // Business rule violation (e.g., invalid parameters)
                    await SendResponseAsync(webSocket, DiscountCodeResult.InvalidRequest);
                    return;
                }
                // Send success response (actual codes not sent in this implementation)
                var response = new GenerateResponse(true);
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                await SendResponseAsync(webSocket, DiscountCodeResult.InvalidRequest);
            }
        }

        /// <summary>
        /// Handles a USE command to mark a discount code as used.
        /// </summary>
        /// <param name="parts">Command arguments: [USE, code]</param>
        /// <param name="webSocket">WebSocket connection to respond to.</param>
        private async Task HandleUseRequest(string[] parts, WebSocket webSocket)
        {
            // Validate argument count
            if (parts.Length == 2)
            {
                // Attempt to use the code via the service
                var resultCode = _discountService.UseCode(parts[1]);
                var response = new UseCodeResponse((byte)resultCode);
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                await SendResponseAsync(webSocket, DiscountCodeResult.InvalidRequest);
            }
        }

        /// <summary>
        /// Sends a simple response indicating the result of a request.
        /// </summary>
        /// <param name="webSocket">WebSocket connection to respond to.</param>
        /// <param name="result">Result code to send.</param>
        private async Task SendResponseAsync(WebSocket webSocket, DiscountCodeResult result)
        {
            await webSocket.SendAsync(
                new ArraySegment<byte>(new[] { (byte)result }),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None
            );
        }
    }
}