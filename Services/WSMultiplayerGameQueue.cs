using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TdA25_Error_Makers.Classes;

namespace TdA25_Error_Makers.Services;

public static class WSMultiplayerGameQueue {

    public record Account {
        public string Name { get; set; }
        public uint Elo { get; set; }
        public WebSocket WebSocket { get; set; }
    }

    private static List<Account> connectedPlayers = new List<Account>();
    private static Timer sortAndPairTimer;
    private static Timer sendQueueCountTimer;

    static WSMultiplayerGameQueue() {
        sortAndPairTimer = new Timer(SortAndPairPlayers, null, 0, 5000);
        //sendQueueCountTimer = new Timer(SendQueueCount, null, 0, 1000);
    }

    public static async Task HandleQueueAsync(WebSocket webSocket) {
        var sessionAccount = HCS.Current.Session.GetObject<Classes.Objects.Account>("loggeduser");
        Account account = new Account() {
            Name = "Guest" + new Random().Next(1000, 9999),
            Elo = 400,
            WebSocket = webSocket
        };

        if (sessionAccount != null) {
            account.Name = sessionAccount.DisplayName;
            account.Elo = sessionAccount.Elo;
        }



        lock (connectedPlayers) {
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

            SendGameLink(player1, player2);
        }
    }

    private static async void SendGameLink(Account player1, Account player2) {
        string gameLink = "/play/multiplayer/ranked/" + Guid.NewGuid();

        var message = Encoding.UTF8.GetBytes(gameLink);
        await player1.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true,
            CancellationToken.None
        );
        await player2.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true,
            CancellationToken.None
        );
    }

    private static void SendQueueCount(object state) {
        var message = Encoding.UTF8.GetBytes(DateTime.Now.ToString("HH:mm:ss"));
        Program.Logger.LogInformation($"Sending time to {connectedPlayers.Count} players: {JsonSerializer.Serialize(connectedPlayers)}");

        lock (connectedPlayers) {
            foreach (var player in connectedPlayers) {
                if (player.WebSocket.State == WebSocketState.Open) {
                    player.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}