using System.Text.Json;
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
    public IActionResult CreateGame([FromBody] Dictionary<string, object> data) {
        if (!data.ContainsKey("name") || !data.ContainsKey("difficulty") || !data.ContainsKey("board")) return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Missing required data." });

        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);

        var name = data["name"].ToString();
        var difficulty = data["difficulty"].ToString();
        var board = data["board"].ToString();

        var createdGame = Game.Create(name, difficulty, board, true);
        if(createdGame == null) return new UnprocessableEntityObjectResult(new { code = BadRequest().StatusCode, message = "Failed to create game." });

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
        if (!data.ContainsKey("name") || !data.ContainsKey("difficulty") || !data.ContainsKey("board")) return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Missing required data." });

        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);

        var name = data["name"].ToString();
        var difficulty = data["difficulty"].ToString();
        var board = data["board"].ToString();

        // aktualizace dat
        using (var updateCmd = new MySqlCommand("UPDATE `games` SET `name` = @name, `difficulty` = @difficulty, `board` = @board WHERE `uuid` = @uuid", conn)) {
            updateCmd.Parameters.AddWithValue("@name", name);
            updateCmd.Parameters.AddWithValue("@difficulty", difficulty);
            updateCmd.Parameters.AddWithValue("@board", board);
            updateCmd.Parameters.AddWithValue("@uuid", uuid);

            int affectedRows = updateCmd.ExecuteNonQuery();
            if (affectedRows == 0) {
                return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Game not found." });
            }
        }

        // načtení hry
        using (var selectCmd = new MySqlCommand("SELECT * FROM `games` WHERE `uuid` = @uuid", conn)) {
            selectCmd.Parameters.AddWithValue("@uuid", uuid);

            using var reader = selectCmd.ExecuteReader();
            if (!reader.Read()) {
                return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Failed to retrieve updated game." });
            }

            var game = new Game(
                reader.GetString("uuid"),
                reader.GetString("name"),
                JsonSerializer.Deserialize<List<List<string>>>(reader.GetValueOrNull<string?>("board") ?? "[]") ?? new List<List<string>>(),
                Enum.Parse<Game.GameDifficulty>(reader.GetString("difficulty")),
                reader.GetDateTime("created_at"),
                reader.GetDateTime("updated_at"),
                Enum.Parse<Game.GameState>(reader.GetString("game_state"))
            );

            return new JsonResult(game){ ContentType = "application/json", StatusCode = 201};
        }
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