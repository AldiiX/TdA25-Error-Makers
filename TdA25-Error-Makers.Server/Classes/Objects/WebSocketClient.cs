using System.Net.WebSockets;

namespace TdA25_Error_Makers.Server.Classes.Objects;

public class WebSocketClient {
    public string UUID { get; set; }
    public WebSocket WebSocket { get; set; }

    public WebSocketClient(WebSocket webSocket, string? uuid = null) {
        WebSocket = webSocket;
        UUID = uuid ?? Guid.NewGuid().ToString();
    }
}