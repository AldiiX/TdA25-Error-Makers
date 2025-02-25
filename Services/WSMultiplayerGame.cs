using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;
using PlayerAccount = TdA25_Error_Makers.Classes.Objects.MultiplayerGame.PlayerAccount;

namespace TdA25_Error_Makers.Services;





public static class WSMultiplayerRankedGame {
    #region Statické proměnné

    private static readonly Dictionary<MultiplayerGame, List<PlayerAccount>> Games = new();
    private static Timer? _statusTimer;
    private static Account? _sessionAccount;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    #endregion

    static WSMultiplayerRankedGame() {
        _statusTimer = new Timer(SendStatus, null, 0, 1000);
    }

    public static async Task HandleAsync(WebSocket webSocket, string gameUUID) {
        _sessionAccount = Utilities.GetLoggedAccountFromContextOrNull() ?? Account.GetByUUID(HCS.Current.Session.GetString("tempAccountUUID") ?? "0");
        var game = HCS.Current.Items["game"] as MultiplayerGame;

        // Ošetření chyb při autorizaci a validaci hry
        if (_sessionAccount == null) {
            await SendErrorAndCloseAsync(webSocket, "Neautorizovaný přístup: uživatel není přihlášen.", WebSocketCloseStatus.PolicyViolation, "Unauthorized");
            return;
        }

        if (game == null) {
            Console.WriteLine(JsonSerializer.Serialize(game));
            await SendErrorAndCloseAsync(webSocket, "Nenalezeno: hra nebyla nalezena.", WebSocketCloseStatus.PolicyViolation, "Not Found");
            return;
        }

        if (game.UUID != gameUUID) {
            await SendErrorAndCloseAsync(webSocket, "Neautorizovaný přístup: nesouhlasí ID hry.", WebSocketCloseStatus.PolicyViolation, "Unauthorized");
            return;
        }

        if (game.State != MultiplayerGame.GameState.RUNNING) {
            await SendErrorAndCloseAsync(webSocket, "Neautorizovaný přístup: hra není spuštěná.", WebSocketCloseStatus.PolicyViolation, "Unauthorized");
            return;
        }

        if (game.PlayerX?.UUID != _sessionAccount.UUID && game.PlayerO?.UUID != _sessionAccount.UUID) {
            await SendErrorAndCloseAsync(webSocket, "Neautorizovaný přístup: uživatel není účastníkem hry.", WebSocketCloseStatus.PolicyViolation, "Unauthorized");
            return;
        }

        // Nastavení času pro hráče
        game.PlayerXTimeLeft = 5 * 60;
        game.PlayerOTimeLeft = 5 * 60;

        // Vytvoření instance hráče a přidání do hry
        var playerAccount = new PlayerAccount(_sessionAccount.UUID, _sessionAccount.DisplayName, _sessionAccount.Elo, webSocket);
        lock (Games) {
            if (!Games.ContainsKey(game))
                Games.Add(game, [playerAccount]);
            else
                Games[game].Add(playerAccount);
        }

        var buffer = new byte[1024 * 4];

        // Smyčka pro příjem zpráv
        while (webSocket.State == WebSocketState.Open) {
            string? message = await ReceiveMessageAsync(webSocket, buffer);
            if (message == null)
                break;

            var jsonNode = JsonNode.Parse(message);
            var action = jsonNode?["action"]?.ToString();
            if (string.IsNullOrEmpty(action))
                continue;

            // Rozdělení zpracování zpráv do pomocných metod
            switch (action) {
                case "MakeMove":
                    await ProcessMakeMove(playerAccount, game, jsonNode);
                    break;
                case "SendChatMessage":
                    await ProcessChatMessage(playerAccount, game, jsonNode);
                    break;
            }
        }

        // Odstranění hráče ze hry a ukončení spojení
        lock (Games) {
            if (Games.TryGetValue(game, out var players)) {
                players.Remove(playerAccount);
                if (players.Count == 0) {
                    _ = MultiplayerGame.EndAsync(gameUUID, null);
                    Games.Remove(game);
                }
            }
        }

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
    }

    #region Zpracování zpráv

    private static async Task ProcessMakeMove(PlayerAccount playerAccount, MultiplayerGame game, JsonNode jsonNode) {
        if (ushort.TryParse(jsonNode?["x"]?.ToString(), out var x) &&
            ushort.TryParse(jsonNode?["y"]?.ToString(), out var y))
        {
            await MakeMove(playerAccount, game, x, y);
        }
    }

    private static async Task ProcessChatMessage(PlayerAccount playerAccount, MultiplayerGame game, JsonNode jsonNode) {
        var chatMsg = jsonNode?["message"]?.ToString();
        var letter = game.PlayerX?.UUID == playerAccount.UUID ? "X" : "O";
        var chatPayload = new {
            action = "chatMessage",
            message = chatMsg,
            sender = playerAccount.Name,
            letter = letter
        };

        var chatBytes = JsonSerializer.SerializeToUtf8Bytes(chatPayload, JsonOptions);
        await BroadcastToGame(game, chatBytes);
    }

    #endregion

    #region Metody pro odesílání zpráv

    private static async Task BroadcastToGame(MultiplayerGame game, byte[] message) {
        List<Task> tasks = [];
        lock (Games) {
            if (Games.TryGetValue(game, out var players)) {
                foreach (var player in players) {
                    if (player.WebSocket != null && player.WebSocket.State == WebSocketState.Open) {
                        tasks.Add(player.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None));
                    }
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    private static async Task SendStatusToPlayer(MultiplayerGame game, PlayerAccount player, int playerCount) {
        var playerTimeLeft = game.PlayerO?.UUID == player.UUID ? game.PlayerOTimeLeft : game.PlayerXTimeLeft;
        var currentPlayer = game.Board.GetNextPlayer();
        var winner = game.Board.GetWinner();

        string? result = null;
        if (winner == GameBoard.Player.X)
            result = game.PlayerX?.UUID == player.UUID ? "win" : "lose";
        else if (winner == GameBoard.Player.O)
            result = game.PlayerO?.UUID == player.UUID ? "win" : "lose";

        var statusPayload = new {
            action = "status",
            playerCount,
            timePlayed = game.GameTime,
            myTimeLeft = playerTimeLeft,
            gameTime = game.GameTime,
            winner = winner switch {
                GameBoard.Player.X => "X",
                GameBoard.Player.O => "O",
                _ => null
            },
            result,
            playerXTimeLeft = game.PlayerXTimeLeft,
            playerOTimeLeft = game.PlayerOTimeLeft,
            yourChar = game.PlayerX?.UUID == player.UUID ? "X" : "O",
            currentPlayer = currentPlayer == GameBoard.Player.X ? "X" : "O"
        };

        var statusBytes = JsonSerializer.SerializeToUtf8Bytes(statusPayload, JsonOptions);
        if (player.WebSocket != null && player.WebSocket.State == WebSocketState.Open) {
            await player.WebSocket.SendAsync(new ArraySegment<byte>(statusBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // Odečítání času, pokud je hráč na řadě
        if (game.Type == MultiplayerGame.GameType.RANKED && game.Board.GetWinner() == null) {
            if (game.PlayerX?.UUID == player.UUID && currentPlayer == GameBoard.Player.X)
                game.PlayerXTimeLeft--;
            if (game.PlayerO?.UUID == player.UUID && currentPlayer == GameBoard.Player.O)
                game.PlayerOTimeLeft--;
        }
    }

    private static async Task BroadcastFinishGame(PlayerAccount winnerPlayer, byte[] winnerMessage, PlayerAccount loserPlayer, byte[] loserMessage) {
        List<Task> finishTasks = [];
        if (winnerPlayer.WebSocket is { State: WebSocketState.Open }) {
            finishTasks.Add(winnerPlayer.WebSocket.SendAsync(new ArraySegment<byte>(winnerMessage), WebSocketMessageType.Text, true, CancellationToken.None));
        }

        if (loserPlayer.WebSocket is { State: WebSocketState.Open }) {
            finishTasks.Add(loserPlayer.WebSocket.SendAsync(new ArraySegment<byte>(loserMessage), WebSocketMessageType.Text, true, CancellationToken.None));
        }

        await Task.WhenAll(finishTasks);
    }

    #endregion

    #region Herní logika

    private static async Task<bool> MakeMove(PlayerAccount playerAccount, MultiplayerGame game, ushort x, ushort y) {
        if (game.Winner != null)
            return false;

        var updatedGame = await MultiplayerGame.ReplaceCellAsync(game.UUID, x, y);
        if (updatedGame == null)
            return false;

        // Aktualizace hracího pole v instanci hry
        lock (Games) {
            var gameKey = Games.Keys.FirstOrDefault(g => g.UUID == game.UUID);
            if (gameKey != null)
                gameKey.Board = updatedGame.Board;
        }

        // Odeslání aktualizované hry všem hráčům
        var updatePayload = new { action = "updateGame", game = updatedGame };
        var updateBytes = JsonSerializer.SerializeToUtf8Bytes(updatePayload, JsonOptions);
        await BroadcastToGame(game, updateBytes);

        // Pokud je výhra, ukonči hru a aktualizuj ELO
        if (updatedGame.Winner != null) {
            await EndGameNormal(updatedGame, playerAccount);
        }

        return true;
    }

    private static void SendStatus(object? state) {

        // Získáme kopii seznamu her, abychom nemuseli iterovat přímo přes dictionary
        List<KeyValuePair<MultiplayerGame, List<PlayerAccount>>> gamesCopy;
        lock (Games) {
            gamesCopy = Games.ToList();
        }

        foreach (var kvp in gamesCopy) {
            var game = kvp.Key;
            var players = kvp.Value;

            // Kontrola, zda některému hráči vypršel čas
            if (game.Type == MultiplayerGame.GameType.RANKED) {
                if (game.PlayerXTimeLeft <= 0) {
                    _ = ForceEndGame(game, "O");
                    continue;
                }

                else if (game.PlayerOTimeLeft <= 0) {
                    _ = ForceEndGame(game, "X");
                    continue;
                }
            }

            // Odeslání status zprávy každému hráči
            foreach (var player in players) {
                _ = SendStatusToPlayer(game, player, players.Count);
            }

            if (game.Board.GetWinner() == null) {
                game.GameTime++;
            }
        }
    }

    private static async Task<bool> ForceEndGame(MultiplayerGame game, string? winnerChar) {
        if (game.EloUpdated)
            return false;

        winnerChar = winnerChar?.ToUpper();
        PlayerAccount? winnerPlayer;
        PlayerAccount? loserPlayer;

        lock (Games) {
            if (!Games.TryGetValue(game, out var players))
                return false;

            if (winnerChar == "X") {
                winnerPlayer = players.Find(p => p.UUID == game.PlayerX?.UUID);
                loserPlayer = players.Find(p => p.UUID == game.PlayerO?.UUID);
            }

            else {
                winnerPlayer = players.Find(p => p.UUID == game.PlayerO?.UUID);
                loserPlayer = players.Find(p => p.UUID == game.PlayerX?.UUID);
            }
        }

        game.Winner = winnerChar == "X" ? GameBoard.Player.X : winnerChar == "O" ? GameBoard.Player.O : null;
        if (winnerPlayer == null || loserPlayer == null)
            return false;

        var winnerAccount = await winnerPlayer.ToFullAccountAsync();
        var loserAccount = await loserPlayer.ToFullAccountAsync();
        if (winnerAccount == null || loserAccount == null)
            return false;

        var (winnerMessage, loserMessage, _, _, _, _) = await CalculateEloAndCreateFinishMessages(game, winnerAccount, loserAccount);
        await BroadcastFinishGame(winnerPlayer, winnerMessage, loserPlayer, loserMessage);

        // Aktualizace databáze
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null)
            return false;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE multiplayer_games SET winner = @winner WHERE uuid = @uuid";
        cmd.Parameters.AddWithValue("@winner", winnerChar);
        cmd.Parameters.AddWithValue("@uuid", game.UUID);

        game.EloUpdated = true;
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static async Task<bool> EndGameNormal(MultiplayerGame game, PlayerAccount playerAccount) {
        PlayerAccount? winnerPlayer;
        PlayerAccount? loserPlayer;
        lock (Games) {
            if (game.PlayerX?.UUID == playerAccount.UUID) {
                winnerPlayer = Games[game].Find(p => p.UUID == game.PlayerX?.UUID);
                loserPlayer = Games[game].Find(p => p.UUID == game.PlayerO?.UUID);
            }

            else {
                winnerPlayer = Games[game].Find(p => p.UUID == game.PlayerO?.UUID);
                loserPlayer = Games[game].Find(p => p.UUID == game.PlayerX?.UUID);
            }
        }

        if (winnerPlayer == null || loserPlayer == null)
            return false;

        Account? winnerAccount = await (game.PlayerX?.UUID == playerAccount.UUID ? game.PlayerX : game.PlayerO)?.ToFullAccountAsync();
        Account? loserAccount = await (game.PlayerX?.UUID == playerAccount.UUID ? game.PlayerO : game.PlayerX)?.ToFullAccountAsync();
        if (winnerAccount == null || loserAccount == null)
            return false;

        var (winnerMessage, loserMessage, _, _, _, _) = await CalculateEloAndCreateFinishMessages(game, winnerAccount, loserAccount);
        await BroadcastFinishGame(winnerPlayer, winnerMessage, loserPlayer, loserMessage);
        return true;
    }

    #endregion

    #region Pomocné metody

    // Zde provádíme výpočet ELO a sestavení finish zpráv.
    private static async Task<(byte[] winnerMessage, byte[] loserMessage, int oldEloWinner, int oldEloLoser, int newEloWinner, int newEloLoser)> CalculateEloAndCreateFinishMessages(MultiplayerGame game, Account winnerAccount, Account loserAccount) {
        int oldEloWinner = (int)winnerAccount.Elo;
        int oldEloLoser = (int)loserAccount.Elo;
        int newEloWinner = (int)winnerAccount.CalculateNewELO(loserAccount, Account.MatchResult.TARGET_WON);
        int newEloLoser = (int)loserAccount.CalculateNewELO(winnerAccount, Account.MatchResult.TARGET_LOST);

        if (game.Type == MultiplayerGame.GameType.RANKED) {
            _ = winnerAccount.UpdateEloInDatabaseAsync((uint)newEloWinner);
            _ = winnerAccount.UpdateWDLInDatabaseAsync(1,0,0);
            _ = loserAccount.UpdateEloInDatabaseAsync((uint)newEloLoser);
            _ = loserAccount.UpdateWDLInDatabaseAsync(0,0,1);
        }

        _ = game.UpdateGameTime();

        var finishWinnerPayload = new {
            action = "finishGame",
            result = "win",
            game,
            elo = newEloWinner,
            oldElo = oldEloWinner,
            gameTime = game.GameTime,
        };

        var finishLoserPayload = new {
            action = "finishGame",
            result = "lose",
            game,
            elo = newEloLoser,
            oldElo = oldEloLoser,
            gameTime = game.GameTime,
        };

        var winnerMessage = JsonSerializer.SerializeToUtf8Bytes(finishWinnerPayload, JsonOptions);
        var loserMessage = JsonSerializer.SerializeToUtf8Bytes(finishLoserPayload, JsonOptions);
        return (winnerMessage, loserMessage, oldEloWinner, oldEloLoser, newEloWinner, newEloLoser);
    }

    private static async Task SendErrorAndCloseAsync(WebSocket socket, string errorMessage, WebSocketCloseStatus status, string closeDescription, CancellationToken cancellationToken = default) {
        var errorPayload = new { error = true, message = errorMessage };
        var errorBytes = JsonSerializer.SerializeToUtf8Bytes(errorPayload, JsonOptions);
        if (socket.State == WebSocketState.Open) {
            await socket.SendAsync(new ArraySegment<byte>(errorBytes), WebSocketMessageType.Text, true, cancellationToken);
            await socket.CloseAsync(status, closeDescription, cancellationToken);
        }
    }

    private static async Task<string?> ReceiveMessageAsync(WebSocket socket, byte[] buffer, CancellationToken cancellationToken = default) {
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        try {
            do {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    return null;
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);
        } catch {
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    #endregion
}