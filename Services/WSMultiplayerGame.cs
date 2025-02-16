using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;
using PlayerAccount = TdA25_Error_Makers.Classes.Objects.MultiplayerGame.PlayerAccount;
// ReSharper disable InconsistentlySynchronizedField
#pragma warning disable CS0169 // Field is never used

namespace TdA25_Error_Makers.Services;





public static class WSMultiplayerRankedGame {

    private static Dictionary<string, List<PlayerAccount>> games = new();
    private static Timer? timer1;

    static WSMultiplayerRankedGame() {
        timer1 = new Timer(CheckGamePlayers!, null, 0, 1000);
    }

    public static async Task HandleAsync(WebSocket webSocket, string gameUUID) {
        var sessionAccount = Utilities.GetLoggedAccountFromContextOrNull();
        var game = HCS.Current.Items["game"] as MultiplayerGame;

        if (sessionAccount == null) {
            await webSocket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(new { error = true, message = "Neautorizovaný přístup: uživatel není přihlášen." }),
                WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unauthorized", CancellationToken.None);
            return;
        }

        if (game == null) {
            Console.WriteLine(JsonSerializer.Serialize(game));
            await webSocket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(new { error = true, message = "Nenalezeno: hra nebyla nalezena." }),
                WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Not Found", CancellationToken.None);
            return;
        }

        if (game.UUID != gameUUID) {
            await webSocket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(new { error = true, message = "Neautorizovaný přístup: nesouhlasí ID hry." }),
                WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unauthorized", CancellationToken.None);
            return;
        }

        if (game.State != MultiplayerGame.GameState.RUNNING) {
            await webSocket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(new { error = true, message = "Neautorizovaný přístup: hra není spuštěná.", c = "UNA1" }),
                WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unauthorized", CancellationToken.None);
            return;
        }

        if (game.PlayerX?.UUID != sessionAccount.UUID && game.PlayerO?.UUID != sessionAccount.UUID) {
            await webSocket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(new { error = true, message = "Neautorizovaný přístup: uživatel není účastníkem hry." }),
                WebSocketMessageType.Text, true, CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unauthorized", CancellationToken.None);
            return;
        }


        PlayerAccount account = new PlayerAccount(
            sessionAccount.UUID,
            sessionAccount.DisplayName,
            sessionAccount.Elo,
            webSocket
        );



        lock (games) {
            if(!games.ContainsKey(gameUUID)) {
                games.Add(gameUUID, [account]);
            }

            else {
                games[gameUUID].Add(account);
            }
        }

        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open) {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);
            var message = Encoding.UTF8.GetString(ms.ToArray());

            if (result.MessageType == WebSocketMessageType.Text) {
                var obj = JsonNode.Parse(message);
                //Console.WriteLine(message);
                if (obj?["action"]?.ToString() == "MakeMove") {
                    var x = ushort.Parse(obj?["x"]?.ToString() ?? "0");
                    var y = ushort.Parse(obj?["y"]?.ToString() ?? "0");

                    await MakeMove(account, game, x, y);
                }
            }
        }

        lock (games) {
            if(games.TryGetValue(gameUUID, out var value))
                value.Remove(account);

            if(games[gameUUID].Count == 0){
                //Console.WriteLine("Game removed");
                _ = MultiplayerGame.EndAsync(gameUUID, null);
                games.Remove(gameUUID);
            }
        }

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
    }


    public static async Task<bool> MakeMove(PlayerAccount account, MultiplayerGame game, ushort x, ushort y) {
        var g = await MultiplayerGame.ReplaceCellAsync(game.UUID, x, y );
        if (g == null) return false;

        var message = JsonSerializer.SerializeToUtf8Bytes(
            new {
                action = "updateGame",
                game = g
            }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        foreach (var player in games[game.UUID]) {
            player.WebSocket?.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }

        return true;
    }


    public static void CheckGamePlayers(object state) {
        lock (games) {
            foreach (var game in games) {
                var gamePlayers = game.Value;

                foreach (var player in gamePlayers) {
                    var message = JsonSerializer.SerializeToUtf8Bytes(
                        new {
                            action = "playersInGame",
                            count = gamePlayers.Count
                        }
                    );

                    player?.WebSocket?.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                }
            }
        }
    }
}