using MySql.Data.MySqlClient;

namespace TdA25_Error_Makers.Classes;





public static class Database {
    public static string DATABASE_IP => Program.ENV.GetValueOrNull("DATABASE_IP") ?? "localhost";
    private const int MAX_POOL_SIZE = 300;





    public static MySqlConnection? GetConnection(bool logError = true) {
        MySqlConnection? conn = null;

        try {
            conn = new MySqlConnection(
                $"server={DATABASE_IP};userid=tda25;password={Program.ENV.GetValueOrNull("DATABASE_PASSWORD") ?? "password"};database=tda25;pooling=true;Max Pool Size={MAX_POOL_SIZE};");
            conn.Open();
        } catch (Exception e) {
            conn?.Close();

            Program.Logger.Log(LogLevel.Error, e, "Database connection error.");
            return null;
        }

        return conn;
    }

    public static async Task<MySqlConnection?> GetConnectionAsync(bool logError = true) {
        MySqlConnection? conn = null;

        try {
            conn = new MySqlConnection(
                $"server={DATABASE_IP};userid=tda25;password={Program.ENV.GetValueOrNull("DATABASE_PASSWORD") ?? "password"};database=tda25;pooling=true;Max Pool Size={MAX_POOL_SIZE};");
            await conn.OpenAsync();
        } catch (Exception e) {
            conn?.CloseAsync();

            Program.Logger.Log(LogLevel.Error, e, "Database connection error.");
            return null;
        }

        return conn;
    }
}