using System.Text.Json;
using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;

namespace TdA25_Error_Makers.Classes.Objects;





public sealed class Account {
    public enum TypeOfAccount { USER, ADMIN, DEVELOPER }

    public string UUID { get; private set; }
    public string Username { get; private set; }
    public string Password { get; private set; }
    public string? Email { get; private set; }
    public string DisplayName { get; private set; }
    public string? Avatar { get; private set; }
    public uint Elo { get; private set; }
    public uint Wins { get; private set; }
    public uint Losses { get; private set; }
    public uint Draws { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TypeOfAccount AccountType { get; private set; }
    public DateTime CreatedAt { get; private set; }



    [JsonConstructor]
    private Account(
        string username, string password, string displayName, string? email, string? avatar, TypeOfAccount accountType,
        uint elo, uint wins, uint losses, uint draws, string uuid, DateTime createdAt
        ) {
        Username = username;
        Password = password;
        Email = email;
        DisplayName = displayName;
        Avatar = avatar;
        AccountType = accountType;
        Elo = elo;
        Wins = wins;
        Losses = losses;
        Draws = draws;
        UUID = uuid;
        CreatedAt = createdAt;
    }

    public enum MatchResult { TARGET_WON, TARGET_LOST, DRAW}
    public static uint CalculateNewELO(Account target, Account b, MatchResult result) {
        // vzorec
        // newElo = oldElo + 40 * ((result - expected) * (1 + 0.5 * (0.5 - (W+D) / (W+D+L) ) ) )

        var res = result switch {
            MatchResult.TARGET_WON => 1,
            MatchResult.TARGET_LOST => 0,
            MatchResult.DRAW or _ => 0.5
        };

        double expected = 1 / (1 + Math.Pow(10, (b.Elo - target.Elo) / 400));
        uint targetNewElo = (uint)Math.Round(target.Elo + 40 * ((res - expected) * (1 + 0.5 * (0.5 - ((double)(target.Wins + target.Draws) / (target.Wins + target.Draws + target.Losses))))));

        return targetNewElo;
    }

    public uint CalculateNewELO(Account b, MatchResult result) => CalculateNewELO(this, b, result);

    public bool UpdateEloInDatabase(uint newElo) {
        var currentElo = this.Elo;
        this.Elo = newElo;

        if (newElo == currentElo) return false;

        using var conn = Database.GetConnection();
        if (conn == null) return false;

        using var cmd = new MySqlCommand("UPDATE `users` SET `elo` = @elo WHERE `uuid` = @uuid", conn);
        cmd.Parameters.AddWithValue("@elo", newElo);
        cmd.Parameters.AddWithValue("@uuid", this.UUID);

        return cmd.ExecuteNonQuery() > 0;
    }

    public override string ToString() => JsonSerializer.Serialize(this);

    public static async Task<Account?> AuthAsync(string username, string hashedPassword) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        await using var cmd = new MySqlCommand($"SELECT * FROM `users` WHERE (`username` = @username OR `email` = @username) AND `password` = @password", conn);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@password", hashedPassword);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !reader.Read()) return null;


        var acc = new Account(
            reader.GetString("username"),
            reader.GetString("password"),
            reader.GetValueOrNull<string>("display_name") ?? reader.GetString("username"),
            reader.GetValueOrNull<string>("email"),
            reader.GetValueOrNull<string>("avatar"),
            Enum.TryParse(reader.GetString("type"), out TypeOfAccount _e) ? _e : TypeOfAccount.USER,
            reader.GetUInt32("elo"),
            reader.GetUInt32("wins"),
            reader.GetUInt32("losses"),
            reader.GetUInt32("draws"),
            reader.GetString("uuid"),
            reader.GetDateTime("created_at")
        );

        HCS.Current.Session.SetObject("loggeduser", acc);
        HCS.Current.Items["loggeduser"] = acc;
        //Console.WriteLine(acc);
        return acc;
    }

    public static Account? Auth(in string username, in string hashedPassword) => AuthAsync(username, hashedPassword).Result;
    
    /* public static async Task<List<Account>> GetAllAsync()
    {
        var list = new List<Account>();
        
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return list;
        
        await using var cmd = new MySqlCommand("SELECT * FROM users", conn);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;

        while (await reader.ReadAsync())
        {
            var user = new Account(
                reader.GetString("username"),
                reader.GetString("password"),
                reader.GetString("email"),
                reader.GetString("display_name"), 
                reader.GetString("avatar"),
                Enum.TryParse<Account.TypeOfAccount>(reader.GetString("type"), out var _e ) ? _e : TypeOfAccount.USER
            );
        }
        
        return list;
    } */
}