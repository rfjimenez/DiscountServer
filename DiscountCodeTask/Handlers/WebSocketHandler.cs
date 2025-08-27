using System.Net.WebSockets;
using System.Text;
using DiscountServer.Models;
using DiscountServer.Services;

namespace DiscountServer.Handlers
{
    public class WebSocketHandler
    {
        private readonly DiscountService _discountService;

        public WebSocketHandler(DiscountService discountService)
        {
            _discountService = discountService;
        }

        public async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }

                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (string.IsNullOrWhiteSpace(receivedMessage))
                {
                    await SendResponseAsync(webSocket, DiscountCodeResult.InvalidRequest);
                }
                else
                {
                    // 🔹 TODO: parse request (Generate or UseCode)
                    // Example placeholder:
                    var codes = _discountService.GenerateCodes(5, 8);
                    await SendResponseAsync(webSocket, DiscountCodeResult.Success);
                }
            }
        }

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