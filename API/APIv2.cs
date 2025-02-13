using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.API;


[ApiController]
[Route("api/v2")]
public class APIv2 : Controller {

    [HttpGet("/api/v2")]
    public IActionResult Index() => new OkObjectResult(new { success = true, message = "API v2" });

    [HttpGet("games")]
    public IActionResult GetGames() {
        var games = Game.GetAll();
        return new JsonResult(games.Where(g => g.IsSaved)) { ContentType = "application/json" };
    }

    [HttpPost("games")]
    public IActionResult CreateGame([FromBody] Dictionary<string, object?> data) {
        if (!data.TryGetValue("name", out object? _name) || !data.TryGetValue("difficulty", out object? _difficulty)) return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Chybí povinná data." });
        if (!data.ContainsKey("board") || data["board"] == null)
            return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Chybí povinná board data." });

        if(string.IsNullOrEmpty(_name?.ToString()) || string.IsNullOrEmpty(_difficulty?.ToString()))
            return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Některá požadovaná data jsou prázdná." });



        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);



        // formátování dat
        string name = _name?.ToString() ?? Game.GenerateRandomGameName();
        string difficulty = _difficulty?.ToString() ?? "medium";
        bool isSaved = data.TryGetValue("saved", out object? _saved) && _saved?.ToString()?.ToLower() == "true";
        bool isInstance = data.TryGetValue("isInstance", out object? _isinstance) && _isinstance?.ToString()?.ToLower() == "true";
        bool errorIfSavingFinishedGame = data.TryGetValue("errorIfSavingCompleted", out object? _errorIfSavingCompleted) && _errorIfSavingCompleted?.ToString()?.ToLower() == "true";
        GameBoard? board =
            !data.TryGetValue("board", out var _bb) ?
                GameBoard.CreateNew() :
            GameBoard.TryParse(_bb?.ToString(), out var _b) ?
                _b : null;

        if (board == null || !board.IsValid()) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Herní plocha není validní." });
        if(errorIfSavingFinishedGame && board.GetWinner() != null)  return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Nemůžeš uložit hru, která je již vyhraná." });



        // vytvoření hry
        var createdGame = Game.Create(name, Game.ParseDifficulty(difficulty), board, isSaved, isInstance, true);
        if(createdGame == null) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Chyba při vytváření hry." });

        return new JsonResult(createdGame){ StatusCode = 201, ContentType = "application/json" };
    }

    [HttpGet("games/generate-name")]
    public IActionResult GenerateGameName() {
        return new OkObjectResult(new { name = Game.GenerateRandomGameName() });
    }

    [HttpGet("games/{uuid}")]
    public IActionResult GetGame(string uuid) {
        var game = Game.GetByUUID(uuid);
        if(game == null) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Hra nebyla nalezena." });

        return new JsonResult(game);
    }

    [HttpPut("games/{uuid}")]
    public IActionResult EditGame(string uuid, [FromBody] Dictionary<string, object?> data) {
        if (!data.TryGetValue("name", out object? _name) || !data.TryGetValue("difficulty", out object? _difficulty) || !data.TryGetValue("board", out object? _board))
            return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Chybí požadovaná data." });

        if(string.IsNullOrEmpty(_name?.ToString()) || string.IsNullOrEmpty(_difficulty?.ToString()))
            return new BadRequestObjectResult(new { code = BadRequest().StatusCode, message = "Některá požadovaná data jsou prázdná." });



        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);
        

        var name = _name?.ToString();
        var difficulty = _difficulty?.ToString();
        var board = _board?.ToString();
        bool? saved = data.TryGetValue("saved", out object? _saved) ? _saved?.ToString()?.ToLower() == "true" : null;
        bool errorIfSavingFinishedGame = data.TryGetValue("errorIfSavingCompleted", out object? _errorIfSavingCompleted) && _errorIfSavingCompleted?.ToString()?.ToLower() == "true";
        bool saveIfFinished = data.TryGetValue("saveIfFinished", out object? _saveIfFinished) && _saveIfFinished?.ToString()?.ToLower() == "true";
        bool? isInstance = data.TryGetValue("isInstance", out object? _isinstance) ? _isinstance?.ToString()?.ToLower() == "true" : null;

        // kontrola validity boardu
        if(!GameBoard.TryParse(board, out var _b)) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Chyba při parsování herní plochy." });
        if (!_b.IsValid()) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Herní plocha není validní." });
        var gameState = _b.GetGameState();
        if(errorIfSavingFinishedGame && _b.GetWinner() != null)  return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Nemůžeš uložit hru, která je již vyhraná." });


        // získání původního boardu pro porovnání
        using var cmd0 = new MySqlCommand("SELECT `game_state` FROM games WHERE uuid = @uuid LIMIT 1", conn);
        cmd0.Parameters.AddWithValue("@uuid", uuid);
        using var reader0 = cmd0.ExecuteReader();
        if (!reader0.Read()) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Hra nebyla nalezena." });
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
                `saved` = IF(@saved IS NOT NULL, @saved, `saved`),
                `is_instance` = IF(@isInstance IS NOT NULL, @isInstance, `is_instance`)
            WHERE `uuid` = @uuid;
            SELECT * FROM games WHERE uuid = @uuid LIMIT 1;
        ", conn);

        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@difficulty", difficulty);
        cmd.Parameters.AddWithValue("@board", board);
        cmd.Parameters.AddWithValue("@actualGameState", saveIfFinished ? "" : actualGameState);
        cmd.Parameters.AddWithValue("@uuid", uuid);
        cmd.Parameters.AddWithValue("@round", _b.GetRound());
        cmd.Parameters.AddWithValue("@gameState", gameState.ToString());
        cmd.Parameters.AddWithValue("@saved", saved);
        cmd.Parameters.AddWithValue("@isInstance", isInstance);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Hra nebyla nalezena." });


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
            reader.GetBoolean("is_instance")
        );

        var gameJson = JsonNode.Parse(JsonSerializer.Serialize(game, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase}));
        if (gameJson == null) return new UnprocessableEntityObjectResult(new { code = UnprocessableEntity().StatusCode, message = "Chyba serializace objektu hry." });

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

        return new OkObjectResult(gameJson);
    }

    [HttpPatch("games/{uuid}")]
    public IActionResult ResetGame(string uuid) {
        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);

        using var cmd = new MySqlCommand(@"
            UPDATE `games`
            SET `board` = @board, `round` = 0, `game_state` = 'OPENING'
            WHERE `uuid` = @uuid;
            SELECT * FROM games WHERE uuid = @uuid LIMIT 1;
        ", conn);

        cmd.Parameters.AddWithValue("@board", GameBoard.CreateNew().ToString());
        cmd.Parameters.AddWithValue("@uuid", uuid);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Hra nebyla nalezena." });

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
            reader.GetBoolean("is_instance")
        );

        return new OkObjectResult(game);
    }

    [HttpDelete("games/{uuid}")]
    public IActionResult DeleteGame(string uuid) {
        using var conn = Database.GetConnection();
        if (conn == null) return new StatusCodeResult(500);

        using var cmd = new MySqlCommand("DELETE FROM `games` WHERE `uuid` = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);

        int affectedRows = cmd.ExecuteNonQuery();
        if (affectedRows == 0) return new NotFoundObjectResult(new { code = NotFound().StatusCode, message = "Hra nebyla nalezena." });

        return new NoContentResult();
    }

    [HttpGet("multiplayer/games")]
    public IActionResult GetMultiplayerGames() {
        var games = MultiplayerGame.GetAll();
        return new JsonResult(games) { ContentType = "application/json" };
    }
}