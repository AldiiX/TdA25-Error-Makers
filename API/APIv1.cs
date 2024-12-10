using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace AdminSphere.API;


[ApiController]
[Route("api/v1")]
public class APIv1 : Controller {

    [HttpGet("hello")]
    public IActionResult Hello() => new OkObjectResult(new { success = true, message = "Hello, World!" });

    [HttpGet("games")]
    public IActionResult GetGames() {
        var games = Game.GetAll();
        return new JsonResult(games) { ContentType = "application/json" };
    }

    [HttpPost("games")]
    public IActionResult CreateGame([FromBody] Dictionary<string, object?> data) {
        if (!data.ContainsKey("name") || !data.ContainsKey("difficulty")) return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Missing required data." });

        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);

        // formátování dat
        string name = data["name"]?.ToString() ?? $"New Game {Random.Shared.Next()}";
        string difficulty = data["difficulty"]?.ToString() ?? "medium";
        string board = data.GetValueOrDefault("board") switch {
            JsonElement jsonElement => jsonElement.ToString(),
            string strValue => strValue,
            _ => "[]"
        };
        string gameState = data.GetValueOrDefault("gameState") switch {
            JsonElement jsonElement => jsonElement.ToString(),
            string strValue => strValue,
            _ => "opening"
        };

        // vytvoření hry
        var createdGame = Game.Create(name, difficulty, board, gameState, true);
        if(createdGame == null) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Failed to create game." });

        return new JsonResult(createdGame){ StatusCode = 201, ContentType = "application/json" };
    }

    [HttpGet("games/{uuid}")]
    public IActionResult GetGame(string uuid) {
        var game = Game.GetByUUID(uuid);
        if(game == null) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Game not found." });

        return new JsonResult(game);
    }

    [HttpPut("games/{uuid}")]
    public IActionResult EditGame(string uuid, [FromBody] Dictionary<string, object> data) {
        if (!data.TryGetValue("name", out object? _name) || !data.TryGetValue("difficulty", out object? _difficulty) || !data.TryGetValue("board", out object? _board)) {
            return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Missing required data." });
        }

        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);
        

        var name = _name.ToString();
        var difficulty = _difficulty.ToString();
        var board = _board.ToString();

        // kontrola validity boardu
        var _b = JsonSerializer.Deserialize<List<List<string>>>(board ?? "[]");
        if (_b == null || _b.Count != 15 || _b.Any(row => row.Count != 15)) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Board is not 15x15." });
        if (!new GameBoard(board).ValidateBoard()) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Invalid board." });



        using var cmd = new MySqlCommand(@"
            UPDATE `games`
            SET 
                `name` = @name,
                `difficulty` = @difficulty,
                `board` = @board,
                `round` = `round` + 1,
                `game_state` = IF(`round` + 1 > 6, 'MIDGAME', `game_state`)
            WHERE `uuid` = @uuid;
            
            SELECT * FROM games;
        ", conn);

        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@difficulty", difficulty);
        cmd.Parameters.AddWithValue("@board", board);
        cmd.Parameters.AddWithValue("@uuid", uuid);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Game not found." });


        var game = new Game(
            reader.GetString("uuid"),
            reader.GetString("name"),
            JsonSerializer.Deserialize<List<List<string>>>(reader.GetValueOrNull<string?>("board") ?? "[]") ?? new List<List<string>>(),
            Enum.Parse<Game.GameDifficulty>(reader.GetString("difficulty")),
            reader.GetDateTime("created_at"),
            reader.GetDateTime("updated_at"),
            Enum.Parse<Game.GameState>(reader.GetString("game_state")),
            reader.GetUInt16("round")
        );

        var gameJson = JsonNode.Parse(JsonSerializer.Serialize(game));
        if (gameJson == null) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Failed to serialize game." });


        // nastavení hry na endgame
        var boardObject = new GameBoard(JsonSerializer.Deserialize<List<List<string>>>(reader.GetValueOrNull<string?>("board")));
        Console.WriteLine("Další tah: " + boardObject.GetNextPlayer());
        Console.WriteLine("Může vyhrát: " + boardObject.CheckIfSomeoneCanWin());
        Console.WriteLine("Vyhrál: " + boardObject.CheckIfSomeoneWon());
        if(boardObject.CheckIfSomeoneCanWin() != null) {
            reader.Close();
            using var endgameCmd = new MySqlCommand("UPDATE `games` SET `game_state` = 'ENDGAME' WHERE `uuid` = @uuid", conn);
            endgameCmd.Parameters.AddWithValue("@uuid", uuid);
            endgameCmd.ExecuteNonQuery();
            gameJson["gameState"] = "endgame";
        } else {
            reader.Close();
            using var endgameCmd = new MySqlCommand($"UPDATE `games` SET `game_state` = {(game.Round > 5 ? "'MIDGAME'" : "'OPENING'")} WHERE `uuid` = @uuid", conn);
            endgameCmd.Parameters.AddWithValue("@uuid", uuid);
            endgameCmd.ExecuteNonQuery();
            gameJson["gameState"] = game.Round > 5 ? "midgame" : "opening";
        }

        return new OkObjectResult(gameJson);
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