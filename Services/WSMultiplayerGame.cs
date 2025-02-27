using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;
using PlayerAccount = TdA25_Error_Makers.Classes.Objects.MultiplayerGame.PlayerAccount;

namespace TdA25_Error_Makers.Services;





public static class WSMultiplayerGame {
    #region Statické proměnné

    public static readonly Dictionary<MultiplayerGame, List<PlayerAccount>> Games = new();
    private static Timer? _statusTimer;
    private static Account? _sessionAccount;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    #endregion

    static WSMultiplayerGame() {
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
        game.PlayerXTimeLeft = 8 * 60;
        game.PlayerOTimeLeft = 8 * 60;

        // Vytvoření instance hráče a přidání do hry
        var playerAccount = new PlayerAccount(_sessionAccount.UUID, _sessionAccount.DisplayName, _sessionAccount.Elo, webSocket);
        lock (Games) {
            if (!Games.TryGetValue(game, out List<PlayerAccount>? value)) Games.Add(game, [playerAccount]);
            else {
                value.Add(playerAccount);
                game = Games.Keys.FirstOrDefault(g => g.UUID == gameUUID);
            }

        }

        if(game == null) {
            await SendErrorAndCloseAsync(webSocket, "Nenalezeno: hra nebyla nalezena.", WebSocketCloseStatus.PolicyViolation, "Not Found");
            return;
        }



        // Smyčka pro příjem zpráv
        while (webSocket.State == WebSocketState.Open) {
            string? message = await ReceiveMessageAsync(webSocket, new byte[1024 * 4]);
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

                case "surrender":
                    await ForceEndGame(game, game.PlayerX?.UUID == playerAccount.UUID ? "O" : "X");
                    break;

                case "requestDraw":
                    await RequestDraw(game, playerAccount);
                    break;

                case "requestRematch":
                    await RequestRematch(game, playerAccount);
                    break;
            }
        }

        // Odstranění hráče ze hry a ukončení spojení
        lock (Games) {
            if (Games.TryGetValue(game, out var players)) {
                players.Remove(playerAccount);
                if (players.Count == 0) {
                    _ = game.EndAndUpdateDatabaseAsync();
                    Games.Remove(game);
                }
            }
        }

        if(playerAccount.WebSocket?.State == WebSocketState.Open) await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
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
        if (game.Type == MultiplayerGame.GameType.RANKED && game.Board.GetWinner() == null && game.State == MultiplayerGame.GameState.RUNNING) {
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

    private static async Task SendMessageAndCloseAsync(PlayerAccount player, byte[] message, string closeDescription) {
        if (player.WebSocket?.State == WebSocketState.Open) {
            await player.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
            await player.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
        }
    }
    #endregion

    #region Herní logika

    private static async Task<bool> MakeMove(PlayerAccount playerAccount, MultiplayerGame game, ushort x, ushort y) {
        if (game.Winner != null)
            return false;

        _ = await game.ReplaceCellAsync(x, y);


        // Odeslání aktualizované hry všem hráčům
        var updatePayload = new { action = "updateGame", game };
        var updateBytes = JsonSerializer.SerializeToUtf8Bytes(updatePayload, JsonOptions);
        await BroadcastToGame(game, updateBytes);

        // Pokud je výhra, ukonči hru a aktualizuj ELO
        if (game is { Winner: not null, State: MultiplayerGame.GameState.RUNNING }) {
            await EndGameNormal(game, playerAccount);
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

            if (game.State == MultiplayerGame.GameState.RUNNING) {
                game.GameTime++;
            }
        }
    }

    private static async Task RequestDraw(MultiplayerGame game, PlayerAccount player) {
        // už jedna žádost o remízu byla
        var votes = game.Votes["drawVotes"];
        if(votes.Contains(player)) return;

        votes.Add(player);

        // poslani upozorneni druhemu hracovi
        var drawPayload = new {
            action = "drawRequest",
            sender = player.Name
        };

        var message = JsonSerializer.SerializeToUtf8Bytes(drawPayload, JsonOptions);
        PlayerAccount? otherPlayer;
        lock(Games) otherPlayer = Games[game].Find(p => p.UUID != player.UUID);

        if(otherPlayer?.WebSocket is { State: WebSocketState.Open })
            await otherPlayer.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);


        // pokud oba hráči hlasovali pro remízu
        if(votes.Count == 2) {
            await ForceEndGameDraw(game);
        }
    }

    private static async Task RequestRematch(MultiplayerGame game, PlayerAccount player) {
        // pokud neni konec hry
        if (game.State != MultiplayerGame.GameState.FINISHED) return;

        var votes = game.Votes["rematchVotes"];
        PlayerAccount? otherPlayer;
        lock(Games) otherPlayer = Games[game].Find(p => p.UUID != player.UUID);

        // pokud druhy hrac neni pripojeny
        if(otherPlayer == null) return;

        // pokud už jedna žádost o rematch byla
        if(votes.Contains(player)) return;

        votes.Add(player);

        // poslani upozorneni druhemu hracovi
        var drawPayload = new {
            action = "rematchRequest",
            sender = player.Name
        };

        var message = JsonSerializer.SerializeToUtf8Bytes(drawPayload, JsonOptions);


        if(otherPlayer?.WebSocket is { State: WebSocketState.Open })
            await otherPlayer.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);


        // pokud oba hráči hlasovali pro remízu
        if(votes.Count == 2 && otherPlayer != null) {
            var newMatch = await MultiplayerGame.CreateAsync(player, otherPlayer, game.Type);
            if (newMatch == null)
                return;

            var payload = new {
                action = "sendToMatch",
                matchUUID = newMatch.UUID
            };

            var rematchMessage = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            await SendMessageAndCloseAsync(player, rematchMessage, "Rematch found");
            await SendMessageAndCloseAsync(otherPlayer, rematchMessage, "Rematch found");
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
            } else {
                winnerPlayer = players.Find(p => p.UUID == game.PlayerO?.UUID);
                loserPlayer = players.Find(p => p.UUID == game.PlayerX?.UUID);
            }
        }

        // Update game winner and state
        game.Winner = winnerChar == "X" ? GameBoard.Player.X : winnerChar == "O" ? GameBoard.Player.O : null;
        game.State = MultiplayerGame.GameState.FINISHED;

        if (winnerPlayer == null || loserPlayer == null)
            return false;

        var winnerAccount = await winnerPlayer.ToFullAccountAsync();
        var loserAccount = await loserPlayer.ToFullAccountAsync();
        if (winnerAccount == null || loserAccount == null)
            return false;

        var (winnerMessage, loserMessage, _, _, _, _) = await CalculateEloAndCreateFinishMessages(game, winnerAccount, loserAccount);
        await BroadcastFinishGame(winnerPlayer, winnerMessage, loserPlayer, loserMessage);

        // updatnuti hry
        await game.UpdateInDatabaseAsync();

        // Update database
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

    private static async Task<bool> ForceEndGameDraw(MultiplayerGame game) {
        if (game.EloUpdated)
            return false;

        PlayerAccount? playerX;
        PlayerAccount? playerO;

        lock (Games) {
            if (!Games.TryGetValue(game, out var players))
                return false;

            playerX = players.Find(p => p.UUID == game.PlayerX?.UUID);
            playerO = players.Find(p => p.UUID == game.PlayerO?.UUID);
        }

        if (playerX == null || playerO == null)
            return false;

        var playerXAccount = await playerX.ToFullAccountAsync();
        var playerOAccount = await playerO.ToFullAccountAsync();
        if (playerXAccount == null || playerOAccount == null)
            return false;


        // upravi se data uzivatelu v databazi
        if (game.Type == MultiplayerGame.GameType.RANKED) {
            _ = playerXAccount.UpdateWDLInDatabaseAsync(0,1,0);
            _ = playerOAccount.UpdateWDLInDatabaseAsync(0,1,0);
        }

        // uprava hry v db
        await game.UpdateInDatabaseAsync();


        var payload = new {
            action = "finishGame",
            result = "draw",
            game,
            //elo = newEloLoser,
            //oldElo = oldEloLoser,
            gameTime = game.GameTime,
        };

        var message = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        await BroadcastFinishGame(playerX, message, playerO, message);


        // Aktualizace databáze
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null)
            return false;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE multiplayer_games SET winner = NULL, state = 'FINISHED' WHERE uuid = @uuid";
        cmd.Parameters.AddWithValue("@uuid", game.UUID);

        game.EloUpdated = true;
        game.Winner = null;
        game.State = MultiplayerGame.GameState.FINISHED;
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

        game.State = MultiplayerGame.GameState.FINISHED;

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

        // uprava hry v db
        await game.UpdateInDatabaseAsync();

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