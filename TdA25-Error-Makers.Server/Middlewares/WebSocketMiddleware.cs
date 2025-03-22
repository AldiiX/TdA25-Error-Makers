using System.Net.WebSockets;
using TdA25_Error_Makers.Server.Classes;
using TdA25_Error_Makers.Server.WebSockets;

namespace TdA25_Error_Makers.Server.Middlewares;

public class WebSocketMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context) {

        // ranked queue websocket
        /*if (context.Request.Path.Value == "/ws/chat") {
            if (context.WebSockets.IsWebSocketRequest) {
                // z query se zjisti kod roomky
                context.Request.EnableBuffering();

                uint? roomNumber = uint.TryParse(context.Request.Query["roomNumber"].FirstOrDefault(), out var _rn)
                    ? _rn : null;

                var acc = Utilities.GetLoggedAccountFromContextOrNull();

                var account = new MultiplayerGame.PlayerAccount(
                    acc?.UUID ?? Guid.NewGuid().ToString(),
                    acc?.DisplayName ?? "Guest " + Guid.NewGuid().ToString()[..6].ToUpper(),
                    acc?.Elo ?? 0,
                    null
                );

                context.Session.SetString("tempAccountUUID", account.UUID);

                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                account.WebSocket = webSocket;

                //await WSMultiplayerFreeplayGameQueue.HandleQueueAsync(webSocket, account, roomNumber);
            } else {
                context.Response.StatusCode = 400;
            }
        }*/

        /*else */await next(context);
    }
}