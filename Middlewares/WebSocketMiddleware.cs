using System.Net.WebSockets;
using TdA25_Error_Makers.Services;

namespace TdA25_Error_Makers.Middlewares;

public class WebSocketMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context) {

        if (context.Request.Path == "/ws/multiplayer/queue") {
            if (context.WebSockets.IsWebSocketRequest) {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await WSMultiplayerGameQueue.HandleQueueAsync(webSocket);
            } else {
                context.Response.StatusCode = 400;
            }
        } else {
            await next(context);
        }
    }
}