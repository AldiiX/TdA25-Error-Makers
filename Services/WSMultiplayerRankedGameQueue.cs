using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Services;

public static class WSMultiplayerRankedGameQueue {
    #region Statické proměnné

    // Seznam připojených hráčů
    private static readonly List<MultiplayerGame.PlayerAccount> connectedPlayers = [];
    private static Timer? sortAndPairTimer;
    //private static Timer? sendQueueCountTimer;

    #endregion

    static WSMultiplayerRankedGameQueue() {
        // Spouštíme periodicky třídění a párování hráčů každých 5 sekund
        sortAndPairTimer = new Timer(SortAndPairPlayers!, null, 0, 5000);
    }

    #region Obsluha fronty

    public static async Task HandleQueueAsync(WebSocket webSocket) {
        var sessionAccount = Utilities.GetLoggedAccountFromContextOrNull();
        if (sessionAccount == null) {
            await SendErrorAndCloseAsync(webSocket, "Unauthorized", "Unauthorized", WebSocketCloseStatus.PolicyViolation);
            return;
        }

        // Používáme hodnoty přímo, protože sessionAccount není null.
        var account = new MultiplayerGame.PlayerAccount(
            sessionAccount.UUID,
            sessionAccount.DisplayName,
            sessionAccount.Elo,
            webSocket
        );

        // Kontrola duplicitního připojení provádíme uvnitř zámku, ale await volání provedeme mimo něj.
        bool alreadyInQueue = false;
        lock (connectedPlayers) {
            if (connectedPlayers.Any(a => a.UUID == account.UUID)) alreadyInQueue = true;
            else connectedPlayers.Add(account);
        }

        if (alreadyInQueue) {
            await SendErrorAndCloseAsync(webSocket, "Already in queue", "Already in queue", WebSocketCloseStatus.PolicyViolation);
            return;
        }

        await ReceiveLoopAsync(webSocket);

        lock (connectedPlayers) {
            connectedPlayers.Remove(account);
        }

        if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived) {
            await webSocket.CloseAsync(
                webSocket.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                webSocket.CloseStatusDescription ?? "Closed",
                CancellationToken.None
            );
        }
    }

    // Smyčka pro příjem zpráv ze socketu
    private static async Task ReceiveLoopAsync(WebSocket webSocket) {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        while (!result.CloseStatus.HasValue) {
            // Zde lze přidat zpracování zpráv, pokud je to potřeba
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }
    }

    #endregion

    #region Párování hráčů

    // Metoda spouštěná timerem, která seřadí hráče dle Elo a páruje je po dvojicích.
    private static void SortAndPairPlayers(object state) {
        lock (connectedPlayers) {
            connectedPlayers.Sort((a, b) => b.Elo.CompareTo(a.Elo));

            for (int i = 0; i < connectedPlayers.Count - 1; i += 2) {
                var player1 = connectedPlayers[i];
                var player2 = connectedPlayers[i + 1];

                // Náhodně prohodíme pořadí s pravděpodobností 50 %
                if (new Random().Next(0, 2) == 0) {
                    (player1, player2) = (player2, player1);
                }

                _ = SendGameLink(player1, player2);
            }
        }
    }

    // Vytvoří zápas a odešle odkaz oběma hráčům, poté uzavře jejich spojení.
    private static async Task SendGameLink(MultiplayerGame.PlayerAccount player1, MultiplayerGame.PlayerAccount player2) {
        var match = await MultiplayerGame.CreateAsync(player1, player2, MultiplayerGame.GameType.RANKED);
        if (match == null)
            return;

        var payload = new { action = "sendToMatch", matchUUID = match.UUID };
        string json = JsonSerializer.Serialize(payload);
        var message = Encoding.UTF8.GetBytes(json);

        await SendMessageAndCloseAsync(player1, message, "Match found");
        await SendMessageAndCloseAsync(player2, message, "Match found");
    }

    #endregion

    #region Pomocné metody

    // Odeslání chybové zprávy a zavření spojení (volá se mimo lock)
    private static async Task SendErrorAndCloseAsync(WebSocket socket, string errorMessage, string closeDescription, WebSocketCloseStatus status, CancellationToken cancellationToken = default) {
        var errorPayload = new { error = true, message = errorMessage };
        var errorBytes = JsonSerializer.SerializeToUtf8Bytes(errorPayload);
        if (socket.State == WebSocketState.Open) {
            await socket.SendAsync(new ArraySegment<byte>(errorBytes), WebSocketMessageType.Text, true, cancellationToken);
            await socket.CloseAsync(status, closeDescription, cancellationToken);
        }
    }

    // Odeslání zprávy hráči a následné uzavření spojení.
    private static async Task SendMessageAndCloseAsync(MultiplayerGame.PlayerAccount player, byte[] message, string closeDescription) {
        if (player.WebSocket?.State == WebSocketState.Open) {
            await player.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
            await player.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
        }
    }

    #endregion
}