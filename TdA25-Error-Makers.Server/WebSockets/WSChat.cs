using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Server.Classes;
using TdA25_Error_Makers.Server.Classes.Objects;


namespace TdA25_Error_Makers.Server.WebSockets;

public static class WSChat {
    private static readonly List<WebSocketClient> ConnectedUsers = [];
    private static readonly List<ChatMessage> Messages = [];
    //private static Timer? statusTimer;

    public class Client : WebSocketClient {
        public string? Name { get; set; }

        public Client(WebSocket webSocket, string name) : base(webSocket) {
            Name = name;
        }
    }


    //handle 
    public static async Task HandleQueueAsync(WebSocket webSocket) {
        var name = HCS.Current.Session.GetString("chatUsername") ?? "Guest " + new Random().Next(0, 9999);
        var client = new Client(webSocket, name);




        lock (ConnectedUsers) ConnectedUsers.Add(client);
        client.SendInicialChat().Wait();



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

                case "chatMessage": {
                    lock(Messages) Messages.Add(new ChatMessage(client.Name,
                            messageJson?["message"]?.ToString() ?? "Unknown"
                        )
                    );

                    lock(ConnectedUsers)
                        foreach (var cl in ConnectedUsers) {
                            var msg = new {
                                action = "message",
                                message = new {
                                    sender = client.Name,
                                    content = messageJson?["message"]?.ToString() ?? "Unknown"
                                }
                            };

                            cl.WebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg))),
                                WebSocketMessageType.Text, true, CancellationToken.None
                            ).Wait();
                        }
                } break;
            }
        }



        // pri ukonceni socketu
        lock (ConnectedUsers) ConnectedUsers.Remove(client);
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
    private static async Task<bool> SendInicialChat(this Client client) {
        await client.BroadcastMessageAsync(JsonSerializer.Serialize(new {
                    action = "init",
                    messages = Messages
                }, new JsonSerializerOptions() {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            )
        );

        return true;
    }

/*static WSChat() {
    statusTimer = new Timer(Status!, null, 0, 1000);
}*/
}