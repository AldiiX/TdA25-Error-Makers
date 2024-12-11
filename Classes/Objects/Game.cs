using System.Text.Json;
using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;

namespace TdA25_Error_Makers.Classes.Objects;





public class Game {

    // picovinky
    public enum GameDifficulty { BEGINNER, EASY, MEDIUM, HARD, EXTREME }
    public enum GameState { OPENING, MIDGAME, ENDGAME, FINISHED }



    // vlastnosti
    public string UUID { get; private set; }
    public string Name { get; private set; }
    public ushort Round { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    [JsonIgnore]
    public GameBoard Board { get; private set; }

    [JsonInclude, JsonPropertyName("board")]
    private List<List<string>> json_Board => Board.ToList();

    [JsonIgnore]
    public GameState State { get; private set; }

    [JsonInclude, JsonPropertyName("gameState")]
    private string json_State => State.ToString().ToLower();

    [JsonIgnore]
    public GameDifficulty Difficulty { get; private set; }

    [JsonInclude, JsonPropertyName("difficulty")]
    private string json_DifficultyLevel => Difficulty.ToString().ToLower();



    // constructory
    public Game(string uuid, string name, GameBoard board, GameDifficulty difficulty, DateTime createdAt, DateTime updatedAt, GameState state, ushort round) {
        UUID = uuid;
        Name = name;
        Difficulty = difficulty;
        Board = board;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        State = state;
        Round = round;
    }



    // static metody
    public static async Task<List<Game>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return [];

        var games = new List<Game>();
        await using var cmd = new MySqlCommand("SELECT * FROM `games`", conn);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null) return games;

        while (await reader.ReadAsync()) {
            games.Add(
                new Game(
                    reader.GetString("uuid"),
                    reader.GetString("name"),
                    GameBoard.Parse(reader.GetValueOrNull<string?>("board")),
                    Enum.Parse<GameDifficulty>(reader.GetString("difficulty")),
                    reader.GetDateTime("created_at"),
                    reader.GetDateTime("updated_at"),
                    Enum.Parse<GameState>(reader.GetString("game_state")),
                    reader.GetUInt16("round")
                )
            );
        }

        return games;
    }

    public static List<Game> GetAll() => GetAllAsync().Result;

    public static async Task<Game?> GetByUUIDAsync(string uuid) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        await using var cmd = new MySqlCommand("SELECT * FROM `games` WHERE `uuid` = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null || !await reader.ReadAsync()) return null;

        return new Game(
            reader.GetString("uuid"),
            reader.GetString("name"),
            GameBoard.Parse(reader.GetValueOrNull<string?>("board")),
            Enum.Parse<GameDifficulty>(reader.GetString("difficulty")),
            reader.GetDateTime("created_at"),
            reader.GetDateTime("updated_at"),
            Enum.Parse<GameState>(reader.GetString("game_state")),
            reader.GetUInt16("round")
        );
    }

    public static Game? GetByUUID(in string uuid) => GetByUUIDAsync(uuid).Result;

    public static Game? Create(string name, string difficulty, GameBoard board, bool insertToDatabase = false) {

        // zpracování boardy
        if (!board.IsValid()) return null;
        GameState gmss = board.GetRound() + 1 > 6 ? GameState.MIDGAME : GameState.OPENING;
        if(board.CheckIfSomeoneCanWin() != null || board.CheckIfSomeoneWon() != null) gmss = GameState.ENDGAME;

        var game = new Game(
            Guid.NewGuid().ToString(),
            name,
            board,
            !Enum.TryParse<GameDifficulty>(difficulty.ToUpper(), out var diff) ? GameDifficulty.BEGINNER : diff,
            DateTime.Now,
            DateTime.Now,
            gmss,
            board.GetRound()
        );




        using var conn = Database.GetConnection();
        if (conn == null) return null;

        using var cmd = new MySqlCommand("INSERT INTO `games` (`uuid`, `name`, `difficulty`, `board`, `created_at`, `updated_at`, `game_state`, `round`) VALUES (@uuid, @name, @difficulty, @board, @created_at, @updated_at, @game_state, @round)", conn);
        cmd.Parameters.AddWithValue("@uuid", game.UUID);
        cmd.Parameters.AddWithValue("@name", game.Name);
        cmd.Parameters.AddWithValue("@difficulty", game.Difficulty.ToString());
        cmd.Parameters.AddWithValue("@board", game.Board.ToString());
        cmd.Parameters.AddWithValue("@created_at", game.CreatedAt);
        cmd.Parameters.AddWithValue("@updated_at", game.UpdatedAt);
        cmd.Parameters.AddWithValue("@game_state", gmss.ToString());
        cmd.Parameters.AddWithValue("@round", board.GetRound());

        int res = 0;
        try {
            res = cmd.ExecuteNonQuery();
        } catch (Exception e) {
            Program.Logger.Log(LogLevel.Error, "Failed to insert new game to database: " + e.Message);
        }

        return res <= 0 ? null : game;
    }
}