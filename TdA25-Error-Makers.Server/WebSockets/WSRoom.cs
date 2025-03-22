using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Server.Classes;
using TdA25_Error_Makers.Server.Classes.Objects;


namespace TdA25_Error_Makers.Server.WebSockets;

public static class WSRoom {
    private static readonly List<Room> Rooms = [];
    //private static Timer? statusTimer;

    public class Client : WebSocketClient {
        public string? Name { get; set; }

        public Client(WebSocket webSocket, string name) : base(webSocket) {
            Name = name;
        }
    }


    //handle
    public static async Task HandleQueueAsync(WebSocket webSocket, Account loggedAccount, Client client, string? roomNumber ) {

        // zjisteni roomky
        Room? room = null;
        if (roomNumber != null) {
            lock (Rooms) room = Rooms.FirstOrDefault(r => r.Code == roomNumber);
        }

        else {
            if (loggedAccount.Username != "spravce") return;

            lock (Rooms) {
                room = new Room();
                Rooms.Add(room);
            }
        }

        if (room == null) return;
        client.SendInitialMessage();




        lock (room.ConnectedUsers) room.ConnectedUsers.Add(client);



        // zpracovani prijmutych zprav
        while (webSocket.State == WebSocketState.Open) {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;

            try {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            } catch (WebSocketException) {
                break;
            }

            // zpracovani zpravy
            string messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (string.IsNullOrWhiteSpace(messageString)) continue;

            JsonNode? messageJson;
            try {
                messageJson = JsonNode.Parse(messageString);
            } catch (JsonException) {
                continue;
            }

            var action = messageJson?["action"]?.ToString();
            if (action == null) continue;

            Program.Logger.LogInformation(messageJson?.ToString());

            switch (action) {
                case "status":
                    /*await client.SendFullReservationInfoAsync();*/
                    break;
            }
        }



        // pri ukonceni socketu
        lock (room.ConnectedUsers) room.ConnectedUsers.Remove(client);
    }

    //metodiky
    private static async Task BroadcastMessageAsync(this Client client, string message) {
        if (client.WebSocket is not { State: WebSocketState.Open }) return;


        var buffer = Encoding.UTF8.GetBytes(message);
        var tasks = new List<Task>();

        client.WebSocket?.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true,
            CancellationToken.None
        );
    }

    //logisticky metody
    private static void SendInitialMessage(this Client client) {
        byte[] message;

        lock (Rooms) {
            message = JsonSerializer.SerializeToUtf8Bytes(new {
                action = "init",
                room = Rooms.Find(r => r.ConnectedUsers.Contains(client))
            });
        }

        client.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
    }


/*static WSChat() {
    statusTimer = new Timer(Status!, null, 0, 1000);
}*/
}