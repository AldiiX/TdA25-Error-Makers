using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TdA25_Error_Makers.Classes.Objects;
using PlayerAccount = TdA25_Error_Makers.Classes.Objects.MultiplayerGame.PlayerAccount;
using Room = TdA25_Error_Makers.Classes.Objects.MultiplayerGame.FreeplayRoom;

namespace TdA25_Error_Makers.Services;

public static class WSMultiplayerFreeplayGameQueue {
    #region Statické proměnné

    // Seznam připojených hráčů
    private static readonly List<Room> Rooms = [];
    private static Timer? timer1;
    //private static Timer? sendQueueCountTimer;

    #endregion

    static WSMultiplayerFreeplayGameQueue() {
        timer1 = new Timer(SendStatus, null, 0, 1000);
    }

    #region Obsluha fronty

    public static async Task HandleQueueAsync(WebSocket webSocket, PlayerAccount account, uint? forceRoomNumber = null) {

        // vytvoření room čísla (6 čísel)
        uint roomNumber = forceRoomNumber ?? 0;
        lock (Rooms) {
            if (forceRoomNumber == null) {
                uint attempts = 0;
                do {
                    if (attempts++ > 999_999) {
                        SendErrorAndCloseAsync(webSocket, "Failed to create room", "Failed to create room", WebSocketCloseStatus.InternalServerError).Wait();
                        return;
                    }

                    roomNumber = (uint) new Random().Next(100_000, 999_999);
                } while (Rooms.Find(x => x.Number == roomNumber) != null);
            }

            // pokud se hráč chce někam připojit, ale místnost neexistuje
            else if (!Rooms.Exists(x => x.Number == roomNumber)) {
                SendMessageAndCloseAsync(account, JsonSerializer.SerializeToUtf8Bytes(new { action = "lobbyDoesntExistError", message = $"Lobby s kódem {roomNumber} neexistuje" }), "Room does not exist").Wait();
            }
        }

        // Kontrola duplicitního připojení provádíme uvnitř zámku, ale await volání provedeme mimo něj.
        bool alreadyInQueue = false;
        lock (Rooms) {
            if (Rooms.Any(room => room.Players.Any(p => p.UUID == account.UUID))) {
                alreadyInQueue = true;
            } else {
                if (Rooms.Find(x => x.Number == roomNumber) == null) {
                    Rooms.Add(new Room(roomNumber, [], account));
                }

                Rooms.Find(x => x.Number == roomNumber)?.Players.Add(account);
            }
        }

        if (alreadyInQueue) {
            await SendErrorAndCloseAsync(webSocket, "Nemůžeš se připojit do lobby, protože už v jednom jsi.", "Already in queue", WebSocketCloseStatus.PolicyViolation);

            return;
        }



        // odeslání potvrzující zprávy
        lock(Rooms) {
            var payload = new {
                action = "joined",
                roomNumber,
                yourAccount = new {
                    account.UUID,
                    account.Name,
                    isOwnerOfRoom = Rooms.Find(x => x.Number == roomNumber)?.Owner.UUID == account.UUID,
                },
                roomOwner = new {
                    Rooms.Find(x => x.Number == roomNumber)?.Owner.UUID,
                    Rooms.Find(x => x.Number == roomNumber)?.Owner.Name
                },
            };

            var message = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions(){ PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            if(webSocket.State == WebSocketState.Open) webSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }




        // posílání zpráv z frontendu na backend
        while (webSocket.State == WebSocketState.Open) {
            string? message = await ReceiveMessageAsync(webSocket, new byte[4 * 1024]);
            if (message == null)
                break;

            var jsonNode = JsonNode.Parse(message);
            var action = jsonNode?["action"]?.ToString();
            if (string.IsNullOrEmpty(action))
                continue;

            // Rozdělení zpracování zpráv do pomocných metod
            switch (action) {
                case "startFreeplayLobby":
                    await StartFreeplayLobbyAsync(account, roomNumber);
                break;
            }
        }



        // odtud se spousti kod pokud uzivatel odpoji websocket
        lock (Rooms) {
            var room = Rooms.Find(x => x.Number == roomNumber);
            room?.Players.Remove(room?.Players.Find(x => x.UUID == account.UUID) ?? account);

            if (room?.Players.Count == 0) Rooms.Remove(room);
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
            var message = ReceiveMessageAsync(webSocket, buffer, CancellationToken.None).Result;
        }
    }

    #endregion

    #region Párování hráčů

    private static async Task StartFreeplayLobbyAsync(PlayerAccount account, uint roomNumber) {
        PlayerAccount? player1;
        PlayerAccount? player2;


        lock (Rooms) {
            var room = Rooms.Find(x => x.Number == roomNumber);
            if (room == null || !Rooms.Contains(room) || room.Players.Count < 2) return;


            var players = room.Players;
            player1 = players[0];
            player2 = players[1];

            // 50% šance na to, že se hráči prohodí
            if (new Random().Next(0, 2) == 0) {
                (player1, player2) = (player2, player1);
            }

            // kicknutí zbývajících hráčů z lobby a odstranění lobby
            foreach (var player in players) {
                if (player == player1 || player == player2) continue;
                var msg = JsonSerializer.SerializeToUtf8Bytes(new { action = "kicked", message = "Lobby odstraněno" });
                SendMessageAndCloseAsync(player, msg, "Vyhozeno z lobby - hra začala").Wait();
            }
        }

        await SendGameLink(player1, player2);
    }

    // Vytvoří zápas a odešle odkaz oběma hráčům, poté uzavře jejich spojení.
    private static async Task SendGameLink(PlayerAccount player1, PlayerAccount player2) {
        var match = await MultiplayerGame.CreateAsync(player1, player2, MultiplayerGame.GameType.FREEPLAY);
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

    private static async void SendStatus(object? state) {
        List<Room> roomsCopy;
        lock (Rooms) {
            roomsCopy = [..Rooms];
        }

        foreach (var room in roomsCopy) {
            var roomNumber = room.Number;
            var players = room.Players;

            // If the room owner is not connected, remove the room.
            if (!players.Contains(room.Owner)) {
                foreach (var player in players) {
                    // pokud je websocket zavreny, tak to hrace odpoji
                    if (player.WebSocket?.State != WebSocketState.Open) {
                        lock(Rooms) Rooms.Find(r => r.Number == room.Number)?.Players.Remove(player);
                        continue;
                    }

                    var payload = new { action = "kicked", message = "Lobby has been cancelled." };
                    var msg = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    if (player.WebSocket?.State == WebSocketState.Open) {
                        await player.WebSocket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
                        await player.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Lobby closed", CancellationToken.None);
                    }
                }

                lock (Rooms) Rooms.Remove(room);
                continue;
            }

            var payloadStatus = new {
                action = "status",
                roomNumber,
                players = players.Select(player => new {
                    player.UUID,
                    player.Name
                }),
                roomOwner = players.Select(player => new {
                    player.UUID,
                    player.Name
                }).FirstOrDefault()
            };

            var message = JsonSerializer.SerializeToUtf8Bytes(payloadStatus, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            foreach (var player in players) {
                if (player.WebSocket?.State == WebSocketState.Open)
                    await player.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    // Odeslání chybové zprávy a zavření spojení (volá se mimo lock)
    private static async Task SendErrorAndCloseAsync(WebSocket socket, string errorMessage, string closeDescription, WebSocketCloseStatus status, CancellationToken cancellationToken = default) {
        var errorPayload = new { action = "error", error = true, message = errorMessage };
        var errorBytes = JsonSerializer.SerializeToUtf8Bytes(errorPayload);
        if (socket.State == WebSocketState.Open) {
            await socket.SendAsync(new ArraySegment<byte>(errorBytes), WebSocketMessageType.Text, true, cancellationToken);
            await socket.CloseAsync(status, closeDescription, cancellationToken);
        }
    }

    // Odeslání zprávy hráči a následné uzavření spojení.
    private static async Task SendMessageAndCloseAsync(PlayerAccount player, byte[] message, string closeDescription) {
        if (player.WebSocket?.State == WebSocketState.Open) {
            await player.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
            await player.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
        }
    }

    private static async Task<string?> ReceiveMessageAsync(WebSocket socket, byte[] buffer, CancellationToken cancellationToken = default) {
        using var ms = new MemoryStream();
        try {
            WebSocketReceiveResult result;
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