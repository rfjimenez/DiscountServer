using DiscountServer.Extensions;
using DiscountServer.Handlers;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Register all your services in one line
builder.Services.AddDiscountServices();

var app = builder.Build();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
        await handler.HandleWebSocketAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

app.Run();
