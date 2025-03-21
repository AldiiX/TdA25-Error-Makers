using System.Net.WebSockets;
using TdA25_Error_Makers.Server.Classes;
using TdA25_Error_Makers.Server.WebSockets;

namespace TdA25_Error_Makers.Server.Middlewares;

public class WebSocketMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context) {

        // ranked queue websocket
        if (context.Request.Path.Value == "/ws/chat") {
            if (context.WebSockets.IsWebSocketRequest) {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await WSChat.HandleQueueAsync(webSocket);
            } else {
                context.Response.StatusCode = 400;
            }
        }

        else await next(context);
    }
}