﻿using MySql.Data.MySqlClient;

namespace TdA25_Error_Makers.Classes;

public static class Database {
    public static string DATABASE_IP => Program.ENV.GetValueOrNull("DATABASE_IP") ?? "localhost";
    private const int MAX_POOL_SIZE = 300;





    public static MySqlConnection? GetConnection(bool logError = true) {
        MySqlConnection? conn = null;

        try {
            conn = new MySqlConnection(
                $"server={DATABASE_IP};userid=tda25;password={Program.ENV["DATABASE_PASSWORD"]};database=tda25;pooling=true;Max Pool Size={MAX_POOL_SIZE};");
            conn.Open();
        } catch (Exception e) {
            conn?.Close();

            Program.Logger.Log(LogLevel.Error, e, "Database connection error.");
            return null;
        }

        return conn;
    }
}