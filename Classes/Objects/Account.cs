using System.Text.Json;
using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;

namespace TdA25_Error_Makers.Classes.Objects;





public sealed class Account {
    public enum TypeOfAccount { USER, ADMIN }

    public string Username { get; private set; }
    public string Password { get; private set; }
    public string? Email { get; private set; }
    public string DisplayName { get; private set; }
    public string? Avatar { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TypeOfAccount AccountType { get; private set; }



    [JsonConstructor]
    private Account(string username, string password, string displayName, string? email, string? avatar, TypeOfAccount accountType) {
        Username = username;
        Password = password;
        Email = email;
        DisplayName = displayName;
        Avatar = avatar;
        AccountType = accountType;
    }

    public override string ToString() => JsonSerializer.Serialize(this);

    public static async Task<Account?> AuthAsync(string username, string hashedPassword) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        await using var cmd = new MySqlCommand($"SELECT * FROM `users` WHERE `username` = @username AND `password` = @password", conn);
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
            Enum.TryParse(reader.GetString("type"), out TypeOfAccount _e) ? _e : TypeOfAccount.USER
        );

        HCS.Current.Session.SetObject("loggeduser", acc);
        HCS.Current.Items["loggeduser"] = acc;
        return acc;
    }

    public static Account? Auth(in string username, in string hashedPassword) => AuthAsync(username, hashedPassword).Result;

}