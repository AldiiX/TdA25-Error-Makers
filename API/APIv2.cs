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

    /*[HttpGet("multiplayer/games")]
    public IActionResult GetMultiplayerGames() {
        var games = MultiplayerGame.GetAll();
        return new JsonResult(games) { ContentType = "application/json" };
    }*/
    
    [HttpPut("credentials")]
public IActionResult UserChangeCredentials([FromBody] Dictionary<string, object?> body) {
    using var conn = Database.GetConnection();
    if (conn == null)
        return new BadRequestObjectResult(new { success = false, message = "Databáze nebyla připojena" });
    
    // Kontrola přihlášení
    var loggedAccount = Auth.ReAuthUser();
    if (loggedAccount == null)
        return new UnauthorizedObjectResult(new { success = false, message = "Musíš být přihlášený." });
    
    // Parsování údajů z body
    var username = body.TryGetValue("username", out var _username) ? _username?.ToString() : null;
    var email = body.TryGetValue("email", out var _email) ? _email?.ToString() : null;
    var displayName = body.TryGetValue("displayName", out var _displayName) ? _displayName?.ToString() : null;
    var oldPassword = body.TryGetValue("password", out var _password) ? _password?.ToString()?.Trim() : null;
    var newPassword = body.TryGetValue("newPassword", out var _newPassword) ? _newPassword?.ToString()?.Trim() : null;
    
    // Pokud chce uživatel změnit heslo, musí zadat obě hesla
    if (!string.IsNullOrEmpty(newPassword) && string.IsNullOrEmpty(oldPassword)) {
        return new BadRequestObjectResult(new { success = false, message = "Pro změnu hesla je třeba zadat staré heslo." });
    }
    if (!string.IsNullOrEmpty(oldPassword) && string.IsNullOrEmpty(newPassword)) {
        return new BadRequestObjectResult(new { success = false, message = "Pro změnu hesla je třeba zadat nové heslo." });
    }
    
    // Aktualizace uživatelských údajů (kromě hesla)
    if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(email) || !string.IsNullOrEmpty(displayName)) {
        string sql = @"
            UPDATE `users` 
            SET 
                `username` = COALESCE(NULLIF(@username, ''), `username`), 
                `email` = COALESCE(NULLIF(@email, ''), `email`),
                `display_name` = COALESCE(NULLIF(@displayName, ''), `display_name`)
            WHERE `uuid` = @uuid;
        ";
        
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uuid", loggedAccount.UUID);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@displayName", displayName);
        cmd.ExecuteNonQuery();
    }
    
    // Pokud se mění heslo, zpracujeme ho v samostatném dotazu
    if (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(oldPassword)) {
        string sql = @"
            UPDATE `users` 
            SET `password` = @newPassword
            WHERE `uuid` = @uuid AND `password` = @oldPassword;
        ";
        
        using var passwordCmd = new MySqlCommand(sql, conn);
        passwordCmd.Parameters.AddWithValue("@uuid", loggedAccount.UUID);
        passwordCmd.Parameters.AddWithValue("@oldPassword", oldPassword);
        passwordCmd.Parameters.AddWithValue("@newPassword", newPassword);
        
        var passwordAffectedRows = passwordCmd.ExecuteNonQuery();
        
        if (passwordAffectedRows == 0)
            return new JsonResult(new { success = false, message = "Špatné staré heslo." });
    }
    
    return new JsonResult(new { success = true, message = "Údaje byly úspěšně změněny." });
}




    [HttpDelete("myaccount")]
    public IActionResult DeleteAccount() {
        using var conn = Database.GetConnection();
        if (conn == null)
            return new BadRequestObjectResult(new { success = false, message = "Databáze nebyla připojena" });
        
        var loggedAccount = Auth.ReAuthUser();
        if (loggedAccount == null)
            return new UnauthorizedObjectResult(new { success = false, message = "Musíš být přihlášený." });
        
        using var cmd = new MySqlCommand("DELETE FROM `users` WHERE `uuid` = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", loggedAccount.UUID);
        
        return cmd.ExecuteNonQuery() > 0 
            ? new JsonResult(new { success = true, message = "Účet byl úspěšně smazán." }) 
            : new JsonResult(new { success = false, message = "Uživatel nebyl nalezen" });
    }
    
    [HttpGet("gamehistory")]
    public IActionResult GetGameHistory() {
        using var conn = Database.GetConnection();
        if (conn == null)
            return new BadRequestObjectResult(new { success = false, message = "Databáze nebyla připojena" });

        var loggedAccount = Auth.ReAuthUser();
        if (loggedAccount == null)
            return new UnauthorizedObjectResult(new { success = false, message = "Musíš být přihlášený." });

        var query = @"
        SELECT mg.*, 
            u1.username AS player_o_username, u2.username AS player_x_username, 
            u1.display_name AS player_o_display_name, u2.display_name AS player_x_display_name
        FROM `multiplayer_games` mg
        LEFT JOIN `users` u1 ON mg.player_o = u1.uuid
        LEFT JOIN `users` u2 ON mg.player_x = u2.uuid
        WHERE (mg.player_o = @uuid OR mg.player_x = @uuid)
        AND FIND_IN_SET('RANKED', mg.type)
        ORDER BY mg.created_at DESC";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@uuid", loggedAccount.UUID);

        using var reader = cmd.ExecuteReader();
        var games = new JsonArray();
        while (reader.Read()) {
            var player = reader.GetString("player_o") == loggedAccount.UUID ? "player_o" : "player_x";
            var opponent = player == "player_o" ? "player_x" : "player_o";
            var loggeduserwon = reader.GetValueOrNull<string>("winner") == "X" && player == "player_x" ||
                                reader.GetValueOrNull<string>("winner") == "O" && player == "player_o";

            var playerOName = reader.GetValueOrNull<string>("player_o_display_name") ?? reader.GetValueOrNull<string>("player_o_username") ?? "Neznámý hráč";
            var playerXName = reader.GetValueOrNull<string>("player_x_display_name") ?? reader.GetValueOrNull<string>("player_x_username") ?? "Neznámý hráč";

            var game = new JsonObject() {
                { "uuid", reader.GetString("uuid") },
                { "board", JsonNode.Parse(reader.GetString("board")) },
                { "winner", reader.GetValueOrNull<string>("winner") ?? "" },
                { "player", player },
                { "opponent", reader.GetValueOrNull<string>(opponent) ?? "neznámý" },
                { "loggeduserwon", loggeduserwon },
                { "created_at", reader.GetDateTime("created_at") },
                { "updated_at", reader.GetDateTime("updated_at") },
                { "player_o", reader.GetValueOrNull<string>("player_o") ?? "neznámý" },
                { "player_x", reader.GetValueOrNull<string>("player_x") ?? "neznámý" },
                { "player_o_name", playerOName },
                { "player_x_name", playerXName },
            };

            games.Add(game);
        }
        return new JsonResult(games);
    }


    
    [HttpGet("users")]
    public IActionResult GetUsers() {
        using var conn = Database.GetConnection();
        if (conn == null)
            return new BadRequestObjectResult(new { success = false, message = "Databáze nebyla připojena" });
        
        var loggedAccount = Auth.ReAuthUser();
        if (loggedAccount == null)
            return new UnauthorizedObjectResult(new { success = false, message = "Musíš být přihlášený." });

        var query = @"
        SELECT * FROM `users` WHERE `temporary` = 0 ORDER BY `username` ASC ";
        
        using var cmd = new MySqlCommand(query, conn);
        
        using var reader = cmd.ExecuteReader();
        var users = new JsonArray();
        while (reader.Read()) {
            var user = new JsonObject() {
                {"uuid", reader.GetString("uuid")},
                {"username", reader.GetString("username")},
                {"display_name", reader.GetValueOrNull<string>("display_name")},
                {"elo", reader.GetInt32("elo")},
                {"created_at", reader.GetDateTime("created_at")},
                {"account_type", reader.GetString("type")},
                {"is_banned", reader.GetValueOrNull<DateTime?>("is_banned") != null && reader.GetDateTime("is_banned") > DateTime.Now}
            };
            users.Add(user);
        }
        return new JsonResult(users);
    }

    private bool BanOrUnban(bool ban, string uuid) {
        using var conn = Database.GetConnection();
        if (conn == null)
            return false;
        
        var loggedAccount = Auth.ReAuthUser();
        if (loggedAccount == null)
            return false;

        if (loggedAccount.AccountType is not (Account.TypeOfAccount.ADMIN or Account.TypeOfAccount.DEVELOPER) )
            return false;
        
        if (loggedAccount.UUID == uuid)
            return false;
        
        var query = @"
        UPDATE `users` SET `is_banned` = @date WHERE `uuid` = @uuid AND `type` = 'USER'";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);
        cmd.Parameters.AddWithValue("@date", ban ? DateTime.Now.AddYears(100) : DBNull.Value);
        
        return cmd.ExecuteNonQuery() > 0;
        
    }
    
    [HttpPut("users/{uuid}/ban")]
    public IActionResult BanUser(string uuid) {
        return BanOrUnban(true, uuid) 
            ? new JsonResult(new { success = true, message = "Uživatel byl zabanován." }) 
            : new JsonResult(new { success = false, message = "Uživatel nebyl zabanován." }) { StatusCode = 400 };
    }

    [HttpPut("users/{uuid}/unban")]
    public IActionResult UnbanUser(string uuid) {
        return BanOrUnban(false, uuid) 
            ? new JsonResult(new { success = true, message = "Uživatel byl odbanován." }) 
            : new JsonResult(new { success = false, message = "Uživatel nebyl odbanován." }) { StatusCode = 400 };
    }

    [HttpGet("leaderboard")]
    public IActionResult GetLeaderboard() {
        var loggedUser = Auth.ReAuthUser();
        using var conn = Database.GetConnection();
        if (conn == null)
            return BadRequest(new { success = false, message = "Databáze nebyla připojena" });

        var query = "SELECT * FROM `users` WHERE `temporary` = 0 ORDER BY `elo` DESC";
        using var cmd = new MySqlCommand(query, conn);
        using var reader = cmd.ExecuteReader();

        var users = new List<JsonObject>();
        JsonObject? userEntry = null;
        int userRank = 0;
        int currentRank = 1;

        while (reader.Read()) {
            var user = new JsonObject {
                { "uuid", reader.GetString("uuid") },
                { "username", reader.GetString("username") },
                { "display_name", reader.GetValueOrNull<string>("display_name") },
                { "elo", reader.GetInt32("elo") },
                { "rank", currentRank }
            };

            if (user["uuid"]?.ToString() == loggedUser?.UUID) {
                userEntry = user;
                userRank = currentRank;
            }

            if (users.Count < 10)
                users.Add(user);

            currentRank++;
        }

        if (userEntry != null && users.All(u => u["uuid"]?.ToString() != loggedUser?.UUID)) {
            users.Add(userEntry);
        }

        return new JsonResult(users);
    }


}