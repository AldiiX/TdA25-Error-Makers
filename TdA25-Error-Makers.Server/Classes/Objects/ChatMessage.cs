namespace TdA25_Error_Makers.Server.Classes.Objects;

public class ChatMessage {
    public string UUID { get; set; }
    public string Sender { get; set; }
    public string Content { get; set; }
    public DateTime Time { get; set; }

    public ChatMessage(string sender, string content) {
        UUID = Guid.NewGuid().ToString();
        Sender = sender;
        Content = content;
        Time = DateTime.Now;
    }
}