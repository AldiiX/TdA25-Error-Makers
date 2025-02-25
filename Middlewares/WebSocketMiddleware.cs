using System.Net.WebSockets;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;
using TdA25_Error_Makers.Services;

namespace TdA25_Error_Makers.Middlewares;

public class WebSocketMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context) {

        // ranked queue websocket
        if (context.Request.Path == "/ws/multiplayer/ranked/queue") {
            if (context.WebSockets.IsWebSocketRequest) {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await WSMultiplayerRankedGameQueue.HandleQueueAsync(webSocket);
            } else {
                context.Response.StatusCode = 400;
            }
        }

        // freeplay queue websocket
        else if (context.Request.Path == "/ws/multiplayer/freeplay/queue") {
            if (context.WebSockets.IsWebSocketRequest) {
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

                await WSMultiplayerFreeplayGameQueue.HandleQueueAsync(webSocket, account, roomNumber);
            } else {
                context.Response.StatusCode = 400;
            }
        }

        // multiplayer (freeplay/ranked) game websocket
        else if(context.Request.Path.Value?.StartsWith("/ws/multiplayer/game/") == true) {
            if (context.WebSockets.IsWebSocketRequest) {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                string gameUUID = context.Request.Path.Value.Split('/').Last();
                await WSMultiplayerRankedGame.HandleAsync(webSocket, gameUUID);
            } else {
                context.Response.StatusCode = 400;
            }
        }

        else await next(context);
    }
}