using TdA25_Error_Makers.Server.WebSockets;

namespace TdA25_Error_Makers.Server.Classes.Objects;

public sealed class Room {
    public List<WSRoom.Client> ConnectedUsers { get; private set; }
    public string Code { get; private set; }

    public Room() {
        Code = new Random().Next(100000, 999999).ToString();
        ConnectedUsers = new List<WSRoom.Client>();
    }
}