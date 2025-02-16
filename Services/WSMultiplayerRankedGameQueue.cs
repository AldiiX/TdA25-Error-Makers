using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;
// ReSharper disable InconsistentlySynchronizedField
#pragma warning disable CS0169 // Field is never used

namespace TdA25_Error_Makers.Services;

public static class WSMultiplayerRankedGameQueue {

    private static List<MultiplayerGame.PlayerAccount> connectedPlayers = [];
    private static Timer? sortAndPairTimer;
    private static Timer? sendQueueCountTimer;

    static WSMultiplayerRankedGameQueue() {
        sortAndPairTimer = new Timer(SortAndPairPlayers!, null, 0, 5000);
        //*sendQueueCountTimer = new Timer(SendQueueCount, null, 0, 1000);
    }

    public static async Task HandleQueueAsync(WebSocket webSocket) {
        var sessionAccount = HCS.Current.Session.GetObject<Classes.Objects.Account>("loggeduser");
        if(sessionAccount == null) {
            await webSocket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { error = true, message = "Unauthorized" }), WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unauthorized", CancellationToken.None);
            return;
        }

        MultiplayerGame.PlayerAccount account = new MultiplayerGame.PlayerAccount(
            sessionAccount?.UUID ?? Guid.NewGuid().ToString(),
            sessionAccount?.DisplayName ?? "Guest " + Guid.NewGuid().ToString().Substring(0, 4),
            sessionAccount?.Elo ?? 1000,
            webSocket
        );



        lock (connectedPlayers) {
            // pokud hráč v listu už je
            if (connectedPlayers.Any(a => a.UUID == account.UUID)) {
                webSocket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { error = true, message = "Already in queue" }), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Already in queue", CancellationToken.None).Wait();
                return;
            }

            connectedPlayers.Add(account);
        }

        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        while (!result.CloseStatus.HasValue) {
            // Process the received message (if needed)
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        lock (connectedPlayers) {
            connectedPlayers.Remove(account);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private static void SortAndPairPlayers(object state) {
        connectedPlayers = connectedPlayers.OrderByDescending(a => a.Elo).ToList();

        for (int i = 0; i < connectedPlayers.Count - 1; i += 2) {
            var player1 = connectedPlayers[i];
            var player2 = connectedPlayers[i + 1];

            // náhodně se vybere, kdo začne
            if (new Random().Next(0, 2) == 0) {
                (player1, player2) = (player2, player1);
            }

            SendGameLink(player1, player2);
        }
    }

    private static async Task SendGameLink(MultiplayerGame.PlayerAccount player1, MultiplayerGame.PlayerAccount player2) {
        var match = await MultiplayerGame.CreateAsync(player1, player2, MultiplayerGame.GameType.RANKED);
        if (match == null) return;

        string obj = JsonSerializer.Serialize(new { action = "sendToMatch", matchUUID = match.UUID });


        var message = Encoding.UTF8.GetBytes(obj);
        await player1.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true,
            CancellationToken.None
        );
        await player2.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true,
            CancellationToken.None
        );
    }

    /*private static void SendQueueCount(object state) {
        var message = Encoding.UTF8.GetBytes(DateTime.Now.ToString("HH:mm:ss"));
        Program.Logger.LogInformation($"Sending time to {connectedPlayers.Count} players: {JsonSerializer.Serialize(connectedPlayers)}");

        lock (connectedPlayers) {
            foreach (var player in connectedPlayers) {
                if (player.WebSocket.State == WebSocketState.Open) {
                    player.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }*/
}