using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.API;


[ApiController]
[Route("api/v1")]
public class APIv1 : Controller {

    [HttpGet("/api/v1")]
    public IActionResult Index() => new OkObjectResult(new { success = true, message = "API v1" });

    [HttpGet("hello")]
    public IActionResult Hello() => new OkObjectResult(new { success = true, message = "Hello, World!" });

    [HttpGet("games")]
    public IActionResult GetGames() {
        var games = Game.GetAll();
        var array = new JsonArray();
        foreach (var game in games) {
            var obj = new {
                uuid = game.UUID,
                createdAt = game.CreatedAt,
                updatedAt = game.UpdatedAt,
                name = game.Name,
                difficulty = game.Difficulty.ToString().ToLower(),
                gameState = game.State.ToString().ToLower(),
                board = game.Board.ToList(),
            };

            array.Add(obj);
        }

        return new JsonResult(array) { ContentType = "application/json" };
    }

    [HttpPost("games")]
    public IActionResult CreateGame([FromBody] Dictionary<string, object?> data) {
        if (!data.ContainsKey("name") || !data.ContainsKey("difficulty")) return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Missing required data." });
        if (!data.ContainsKey("board") || data["board"] == null)
            return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Missing required board data." });

        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);



        // formátování dat
        string name = data["name"]?.ToString() ?? Game.GenerateRandomGameName();
        string difficulty = data["difficulty"]?.ToString() ?? "medium";
        const bool isSaved = true;
        GameBoard? board =
            !data.TryGetValue("board", out var _bb) ?
                GameBoard.CreateNew() :
            GameBoard.TryParse(_bb?.ToString(), out var _b) ?
                _b : null;

        if (board == null || !board.IsValid()) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Board is not valid." });



        // vytvoření hry
        var createdGame = Game.Create(name, Game.ParseDifficulty(difficulty), board, isSaved, false, true);
        if(createdGame == null) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Failed to create game." });

        return new JsonResult(createdGame){ StatusCode = 201, ContentType = "application/json" };
    }

    [HttpGet("games/{uuid}")]
    public IActionResult GetGame(string uuid) {
        var game = Game.GetByUUID(uuid);
        if(game == null) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Game not found." });

        var obj = new JsonObject {
            ["uuid"] = game.UUID,
            ["createdAt"] = game.CreatedAt,
            ["updatedAt"] = game.UpdatedAt,
            ["name"] = game.Name,
            ["difficulty"] = game.Difficulty.ToString().ToLower(),
            ["gameState"] = game.State.ToString().ToLower(),
            ["board"] = game.Board.ToJsonNode(),
        };

        return new JsonResult(obj);
    }

    [HttpPut("games/{uuid}")]
    public IActionResult EditGame(string uuid, [FromBody] Dictionary<string, object?> data) {
        if (!data.TryGetValue("name", out object? _name) || !data.TryGetValue("difficulty", out object? _difficulty) || !data.TryGetValue("board", out object? _board))
            return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Missing required data." });

        if(string.IsNullOrEmpty(_name?.ToString()) || string.IsNullOrEmpty(_difficulty?.ToString()))
            return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Some required data is empty string." });



        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);
        

        var name = _name?.ToString();
        var difficulty = _difficulty?.ToString();
        var board = _board?.ToString();
        const bool saved = true;
        const bool saveIfFinished = true;

        // kontrola validity boardu
        if(!GameBoard.TryParse(board, out var _b)) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Failed to parse board." });
        if (!_b.IsValid()) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Board is not valid." });
        var gameState = _b.GetGameState();


        // získání původního boardu pro porovnání
        using var cmd0 = new MySqlCommand("SELECT `game_state` FROM games WHERE uuid = @uuid LIMIT 1", conn);
        cmd0.Parameters.AddWithValue("@uuid", uuid);
        using var reader0 = cmd0.ExecuteReader();
        if (!reader0.Read()) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Game not found." });
        string actualGameState = reader0.GetString("game_state").ToUpper();
        reader0.Close();


        // updatnutí hry v db a získání nového stavu
        using var cmd = new MySqlCommand(@"
            UPDATE `games`
            SET 
                `name` = @name,
                `difficulty` = @difficulty,
                `board` = IF(@actualGameState = 'FINISHED', `board`, @board),
                `round` = @round,
                `game_state` = @gameState,
                `saved` = IF(@saved IS NOT NULL, @saved, `saved`)
            WHERE `uuid` = @uuid;
            SELECT * FROM games WHERE uuid = @uuid LIMIT 1;
        ", conn);

        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@difficulty", difficulty);
        cmd.Parameters.AddWithValue("@board", board);
        cmd.Parameters.AddWithValue("@actualGameState",  saveIfFinished ? "" : actualGameState);
        cmd.Parameters.AddWithValue("@uuid", uuid);
        cmd.Parameters.AddWithValue("@round", _b.GetRound());
        cmd.Parameters.AddWithValue("@gameState", gameState.ToString());
        cmd.Parameters.AddWithValue("@saved", saved);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Game not found." });


        var game = new Game(
            reader.GetString("uuid"),
            reader.GetString("name"),
            GameBoard.Parse(reader.GetValueOrNull<string?>("board")),
            Enum.Parse<Game.GameDifficulty>(reader.GetString("difficulty")),
            reader.GetDateTime("created_at"),
            reader.GetDateTime("updated_at"),
            Enum.Parse<Game.GameState>(reader.GetString("game_state")),
            reader.GetUInt16("round"),
            reader.GetBoolean("saved"),
            false
        );

        var gameJson = JsonNode.Parse(JsonSerializer.Serialize(game, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase}));
        if (gameJson == null) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Failed to serialize game." });

        // nastavení hry na endgame
        /*Console.WriteLine($"\n---------{DateTime.Now.ToLocalTime()}-----------");
        Console.WriteLine("Board: " + game.Board);
        Console.WriteLine("Aktuální kolo: " + game.Board.GetRound());
        Console.WriteLine("Další tah: " + game.Board.GetNextPlayer());
        Console.WriteLine("Může vyhrát: " + game.Board.CheckIfSomeoneCanWin());
        Console.WriteLine("Vyhrál: " + game.Board.CheckIfSomeoneWon());
        Console.WriteLine("------------------------------------");*/

        // nastavení hry na endgame / finished
        if (game.Board.CheckIfSomeoneWon() != null) {
            reader.Close();
            using var endgameCmd = new MySqlCommand("UPDATE `games` SET `game_state` = 'FINISHED' WHERE `uuid` = @uuid", conn);
            endgameCmd.Parameters.AddWithValue("@uuid", uuid);
            endgameCmd.ExecuteNonQuery();
            gameJson["gameState"] = "finished";
        }

        else if(game.Board.CheckIfSomeoneCanWin() != null) {
            reader.Close();
            using var endgameCmd = new MySqlCommand("UPDATE `games` SET `game_state` = 'ENDGAME' WHERE `uuid` = @uuid", conn);
            endgameCmd.Parameters.AddWithValue("@uuid", uuid);
            endgameCmd.ExecuteNonQuery();
            gameJson["gameState"] = "endgame";
        }

        else {
            reader.Close();
            using var endgameCmd = new MySqlCommand($@"
                UPDATE `games`
                SET `game_state` = IF(`round` + 1 > 6, 'MIDGAME', 'OPENING')
                WHERE `uuid` = @uuid;
            ", conn);
            endgameCmd.Parameters.AddWithValue("@uuid", uuid);
            endgameCmd.ExecuteNonQuery();
            gameJson["gameState"] = game.Round > 5 ? "midgame" : "opening";
        }

        var r = new JsonObject() {
            ["uuid"] = JsonValue.Create(gameJson["uuid"]?.ToString()),
            ["createdAt"] = JsonValue.Create(gameJson["createdAt"]?.ToString()),
            ["updatedAt"] = JsonValue.Create(gameJson["updatedAt"]?.ToString()),
            ["name"] = JsonValue.Create(gameJson["name"]?.ToString()),
            ["difficulty"] = JsonValue.Create(gameJson["difficulty"]?.ToString()),
            ["gameState"] = JsonValue.Create(gameJson["gameState"]?.ToString()),
            ["board"] = gameJson["board"]?.DeepClone(),
        };

        return new OkObjectResult(r);
    }

    [HttpDelete("games/{uuid}")]
    public IActionResult DeleteGame(string uuid) {
        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);

        using var cmd = new MySqlCommand("DELETE FROM `games` WHERE `uuid` = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);

        int affectedRows = cmd.ExecuteNonQuery();
        if (affectedRows == 0) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Game not found." });

        return new NoContentResult();
    }
}