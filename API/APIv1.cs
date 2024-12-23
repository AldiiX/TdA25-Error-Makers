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
        return new JsonResult(games.Where(g => g.IsSaved)) { ContentType = "application/json" };
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
        GameBoard? board =
            !data.TryGetValue("board", out var _bb) ?
                GameBoard.CreateNew() :
            GameBoard.TryParse(_bb?.ToString(), out var _b) ?
                _b : null;

        if (board == null || !board.IsValid()) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Board is not valid." });



        // vytvoření hry
        var createdGame = Game.Create(name, Game.ParseDifficulty(difficulty), board, true, true);
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
    public IActionResult EditGame(string uuid, [FromBody] Dictionary<string, object?> data) {
        if (!data.TryGetValue("name", out object? _name) || !data.TryGetValue("difficulty", out object? _difficulty) || !data.TryGetValue("board", out object? _board)) {
            return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Missing required data." });
        }

        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);
        

        var name = _name?.ToString();
        var difficulty = _difficulty?.ToString();
        var board = _board?.ToString();
        bool? saved = data.TryGetValue("saved", out object? _saved) ? _saved?.ToString()?.ToLower() == "true" : null;

        // kontrola validity boardu
        if(!GameBoard.TryParse(board, out var _b)) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Failed to parse board." });
        if (!_b.IsValid()) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Board is not valid." });
        var gameState = _b.GetGameState();


        using var cmd = new MySqlCommand(@"
            UPDATE `games`
            SET 
                `name` = @name,
                `difficulty` = @difficulty,
                `board` = @board,
                `round` = @round,
                `game_state` = @gameState,
                `saved` = IF(@saved IS NOT NULL, @saved, `saved`)
            WHERE `uuid` = @uuid;
            SELECT * FROM games WHERE uuid = @uuid LIMIT 1;
        ", conn);

        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@difficulty", difficulty);
        cmd.Parameters.AddWithValue("@board", board);
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
            reader.GetBoolean("saved")
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

        if(game.Board.CheckIfSomeoneCanWin() != null) {
            reader.Close();
            using var endgameCmd = new MySqlCommand("UPDATE `games` SET `game_state` = 'ENDGAME' WHERE `uuid` = @uuid", conn);
            endgameCmd.Parameters.AddWithValue("@uuid", uuid);
            endgameCmd.ExecuteNonQuery();
            gameJson["gameState"] = "endgame";
        } else {
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