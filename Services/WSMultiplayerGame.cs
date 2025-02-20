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

    private static Dictionary<MultiplayerGame, List<PlayerAccount>> games = new();
    private static Timer? timer1;
    private static Account? sessionAccount;

    static WSMultiplayerRankedGame() {
        timer1 = new Timer(SendStatus!, null, 0, 1000);
    }

    public static async Task HandleAsync(WebSocket webSocket, string gameUUID) {
        sessionAccount = Utilities.GetLoggedAccountFromContextOrNull();
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

        game.PlayerXTimeLeft = 300;
        game.PlayerOTimeLeft = 300;

        PlayerAccount account = new PlayerAccount(
            sessionAccount.UUID,
            sessionAccount.DisplayName,
            sessionAccount.Elo,
            webSocket
        );



        lock (games) {
            if(!games.ContainsKey(game)) {
                games.Add(game, [account]);
            }

            else {
                games[game].Add(account);
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
                var action = obj?["action"]?.ToString();
                //Console.WriteLine(message);

                switch (action) {
                    case "MakeMove": {
                        var x = ushort.Parse(obj?["x"]?.ToString() ?? "0");
                        var y = ushort.Parse(obj?["y"]?.ToString() ?? "0");

                        await MakeMove(account, game, x, y);
                    } break;

                    case "SendChatMessage": {
                        var msg = JsonSerializer.SerializeToUtf8Bytes(new {
                            action = "chatMessage",
                            message = obj?["message"]?.ToString(),
                            sender = account?.Name,
                            letter = game.PlayerX?.UUID == account?.UUID ? "X" : "O",
                        });

                        foreach (var player in games[game]) player.WebSocket?.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                    } break;
                }
            }
        }

        lock (games) {
            if(games.TryGetValue(game, out var value))
                value.Remove(account);

            if(games[game].Count == 0){
                //Console.WriteLine("Game removed");
                _ = MultiplayerGame.EndAsync(gameUUID, null);
                games.Remove(game);
            }
        }

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
    }


    public static async Task<bool> MakeMove(PlayerAccount account, MultiplayerGame game, ushort x, ushort y) {
        var g = await MultiplayerGame.ReplaceCellAsync(game.UUID, x, y );
        if (g == null) return false;

        // nastavení boardy
        var gameInGamesDict = games.Keys.ToList().Find(k => k.UUID == game.UUID);
        if (gameInGamesDict != null) gameInGamesDict.Board = g.Board;


        var updateMessage = JsonSerializer.SerializeToUtf8Bytes(new {
            action = "updateGame",
            game = g,
        }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        foreach (var player in games[game]) {
            player.WebSocket?.SendAsync(new ArraySegment<byte>(updateMessage), WebSocketMessageType.Text, true,
                CancellationToken.None
            ).Wait();
        }



        // pokud je výhra, spočítá se ELO pro oba uživatele
        if (g.Winner != null) {

            // získání vítěze a poraženého, původní gameobject
            var w = g.PlayerX?.UUID == account.UUID ? games[game].Find(player => player.UUID == g.PlayerX?.UUID) : games[game].Find(player => player.UUID == g.PlayerO?.UUID);
            var l = g.PlayerX?.UUID == account.UUID ? games[game].Find(player => player.UUID == g.PlayerO?.UUID) : games[game].Find(player => player.UUID == g.PlayerX?.UUID);

            // asynchronní získání úplných účtů
            var winnerTask = (g.PlayerX?.UUID == account.UUID ? g.PlayerX : g.PlayerO)?.ToFullAccountAsync();
            var loserTask = (g.PlayerX?.UUID == account.UUID ? g.PlayerO : g.PlayerX)?.ToFullAccountAsync();
            if(winnerTask == null || loserTask == null) return false;

            // získání vítěze a poraženého, úplné účty
            var winner = await winnerTask;
            var loser = await loserTask;
            if(winner == null || loser == null) return false;

            var oldEloWinner = winner.Elo;
            var oldEloLoser = loser.Elo;
            var newEloWinner = winner.CalculateNewELO(loser, Account.MatchResult.TARGET_WON);
            var newEloLoser = loser.CalculateNewELO(winner, Account.MatchResult.TARGET_LOST);

            _ = winner.UpdateEloInDatabaseAsync(newEloWinner);
            _ = loser.UpdateEloInDatabaseAsync(newEloLoser);
            _ = g.UpdateGameTime();

            var msgWinner = JsonSerializer.SerializeToUtf8Bytes(new {
                action = "finishGame",
                oldElo = oldEloWinner,
                elo = newEloWinner,
                result = "win",
                game = g,
                gameTime = game.GameTime,
            }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var msgLoser = JsonSerializer.SerializeToUtf8Bytes(new {
                action = "finishGame",
                oldElo = oldEloLoser,
                elo = newEloLoser,
                result = "lose",
                game = g,
                gameTime = game.GameTime,
            }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            w?.WebSocket?.SendAsync(new ArraySegment<byte>(msgWinner), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
            l?.WebSocket?.SendAsync(new ArraySegment<byte>(msgLoser), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }

        return true;
    }


    public static void SendStatus(object state) {
        lock (games) {
            foreach (var kvp in games) {
                MultiplayerGame game = kvp.Key;
                List<PlayerAccount> gamePlayers = kvp.Value;

                foreach (var player in gamePlayers) {
                    var playerTimeLeft = game.PlayerO?.UUID == player.UUID ? game.PlayerOTimeLeft : game.PlayerXTimeLeft;

                    var message = JsonSerializer.SerializeToUtf8Bytes(
                        new {
                            action = "status",
                            playerCount = gamePlayers.Count,
                            timePlayed = game.GameTime,
                            myTimeLeft = playerTimeLeft,
                            gameTime = game.GameTime,
                        }
                    );

                    player.WebSocket?.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                    // pokud je na řadě tento hráč, odečte se mu čas
                    var currentPlayer = game.Board.GetNextPlayer();
                    bool gameIsFinished = game.Board.GetWinner() != null;
                    if (!gameIsFinished && game.PlayerX?.UUID == player.UUID && currentPlayer == GameBoard.Player.X) game.PlayerXTimeLeft--;
                    if (!gameIsFinished && game.PlayerO?.UUID == player.UUID && currentPlayer == GameBoard.Player.O) game.PlayerOTimeLeft--;
                }

                if(game.Board.GetWinner() == null) game.GameTime++;
            }
        }
    }
}