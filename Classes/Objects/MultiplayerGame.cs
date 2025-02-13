using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;

namespace TdA25_Error_Makers.Classes.Objects;



public class MultiplayerGame {


    // podclassy, enumy
    public enum GameDifficulty { BEGINNER, EASY, MEDIUM, HARD, EXTREME }
    public enum GameState { OPENING, MIDGAME, ENDGAME, FINISHED }

    protected MultiplayerGame(string uuid, ushort round, DateTime createdAt, DateTime updatedAt, string? winner, GameBoard board) {
        UUID = uuid;
        Round = round;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Winner = winner;
        Board = board;
    }


    public string UUID { get; private set; }
    public ushort Round { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? Winner { get; private set; }
    public string CurrentPlayer => Board.GetNextPlayer().ToString().ToUpper();

    [JsonIgnore]
    public GameBoard Board { get; private set; }

    [JsonInclude, JsonPropertyName("board")]
    private List<List<string>> json_Board => Board?.ToList() ?? new List<List<string>>();

    [JsonInclude, JsonPropertyName("winningCells")]
    private HashSet<List<int>>? json_WinningCells {
        get {
            if (Winner == null || Board == null) return null;

            return Board.GetWinningCells()?.Select(cell => new List<int> { cell.row, cell.col }).ToHashSet();
        }
    }




    public static async Task<List<MultiplayerGame>> GetAllAsync() {
        var list = new List<MultiplayerGame>();

        await using var conn = await Database.GetConnectionAsync();
        if(conn == null) return list;

        await using var cmd = new MySqlCommand("SELECT * FROM multiplayer_games", conn);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null) return list;

        while (await reader.ReadAsync()) {
            var game = new MultiplayerGame(
                reader.GetString("uuid"),
                reader.GetUInt16("round"),
                reader.GetDateTime("created_at"),
                reader.GetDateTime("updated_at"),
                reader.GetValueOrNull<string>("winner"),
                GameBoard.Parse(reader.GetValueOrNull<string>("board")!)
            );

            list.Add(game);
        }

        return list;
    }

    public static List<MultiplayerGame> GetAll() => GetAllAsync().Result;
}