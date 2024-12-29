using MySql.Data.MySqlClient;

namespace TdA25_Error_Makers.Classes;





public static class Database {
    public static string DATABASE_IP => Program.ENV.GetValueOrNull("DATABASE_IP") ?? "localhost";
    private static readonly string ORIGINAL_CONNECTION_STRING = $"server={DATABASE_IP};userid=tda25;password={Program.ENV.GetValueOrNull("DATABASE_PASSWORD") ?? "password"};database=tda25;pooling=true;Max Pool Size={MAX_POOL_SIZE};";
    public static string CONNECTION_STRING = ORIGINAL_CONNECTION_STRING + "";
    public static bool LAST_CONNECTION_FAILED = false;
    public const int MAX_POOL_SIZE = 300;
    public static bool IsUsingFallbackServer => CONNECTION_STRING.Contains("localhost");





    public static MySqlConnection? GetConnection(bool logError = true) {
        MySqlConnection? conn = null;

        try {
            conn = new MySqlConnection(CONNECTION_STRING);
            conn.Open();

            if (LAST_CONNECTION_FAILED) {
                Program.Logger.Log(LogLevel.Information, "Database connection successful.");
                LAST_CONNECTION_FAILED = false;
            }
        } catch (Exception e) {
            conn?.Close();

            Program.Logger.Log(LogLevel.Error, $"Database connection „{conn?.DataSource}” error: {e.Message}, trying fallback.");
            SwitchToFallbackServer();

            try {
                conn = new MySqlConnection(CONNECTION_STRING);
                conn.Open();

                Program.Logger.Log(LogLevel.Information, "Fallback database connection successful.");
            } catch (Exception e2) {
                conn?.Close();

                Program.Logger.Log(LogLevel.Error, $"Database connection „{conn?.DataSource}” error: {e2.Message}, fallback failed.");
                SwitchToNormalServer();

                LAST_CONNECTION_FAILED = true;
                return null;
            }

            LAST_CONNECTION_FAILED = true;
            return null;
        }

        return conn;
    }

    public static async Task<MySqlConnection?> GetConnectionAsync(bool logError = true) {
        MySqlConnection? conn = null;

        try {
            conn = new MySqlConnection(CONNECTION_STRING);
            await conn.OpenAsync();

            if (LAST_CONNECTION_FAILED) {
                Program.Logger.Log(LogLevel.Information, "Database connection successful.");
                LAST_CONNECTION_FAILED = false;
            }
        } catch (Exception e) {
            conn?.CloseAsync();

            Program.Logger.Log(LogLevel.Error, $"Database connection „{conn?.DataSource}” error: {e.Message}, trying fallback.");
            SwitchToFallbackServer();

            try {
                conn = new MySqlConnection(CONNECTION_STRING);
                await conn.OpenAsync();

                Program.Logger.Log(LogLevel.Information, "Fallback database connection successful.");
            } catch (Exception e2) {
                conn?.CloseAsync();

                Program.Logger.Log(LogLevel.Error, $"Database connection „{conn?.DataSource}” error: {e2.Message}, fallback failed.");
                SwitchToNormalServer();

                LAST_CONNECTION_FAILED = true;
                return null;
            }


            LAST_CONNECTION_FAILED = true;
            return null;
        }

        return conn;
    }

    public static void SwitchToFallbackServer() {
        CONNECTION_STRING = $"server=localhost;userid=tda25;password=password;database=tda25;pooling=true;Max Pool Size={MAX_POOL_SIZE};";
    }

    public static void SwitchToNormalServer() {
        CONNECTION_STRING = ORIGINAL_CONNECTION_STRING;
    }
}