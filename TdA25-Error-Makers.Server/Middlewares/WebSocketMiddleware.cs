using System.Net.WebSockets;
using TdA25_Error_Makers.Server.Classes;
using TdA25_Error_Makers.Server.WebSockets;

namespace TdA25_Error_Makers.Server.Middlewares;

public class WebSocketMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context) {

        // ranked queue websocket
        if (context.Request.Path.Value == "/ws/room") {
            if (context.WebSockets.IsWebSocketRequest) {
                // z query se zjisti kod roomky
                context.Request.EnableBuffering();

                string? roomNumber = context.Request.Query["roomNumber"];
                string? username = context.Request.Query["username"];

                var loggedAccount = Utilities.GetLoggedAccountFromContextOrNull();
                //if (loggedAccount == null) return;

                var name = loggedAccount?.Username ?? username ?? "Guest" + new Random().Next(1000, 9999);
                var client = new WSRoom.Client(null!, name);

                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                client.WebSocket = webSocket;

                await WSRoom.HandleQueueAsync(webSocket, loggedAccount, client, roomNumber);
            } else {
                context.Response.StatusCode = 400;
            }
        }

        else await next(context);
    }
}