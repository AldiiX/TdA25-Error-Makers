﻿using System.Net.WebSockets;
using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Services;

namespace TdA25_Error_Makers.Classes.Objects;



public class MultiplayerGame {


    // podclassy, enumy
    public enum GameDifficulty { BEGINNER, EASY, MEDIUM, HARD, EXTREME }
    public enum GameState { RUNNING, FINISHED }
    public enum GameType { FREEPLAY, RANKED }

    public record PlayerAccount {
        public string UUID { get; set; }
        public string Name { get; set; }
        public uint Elo { get; set; }
        public WebSocket? WebSocket { get; set; }

        public PlayerAccount(string uuid, string name, uint elo, WebSocket? webSocket = null) {
            UUID = uuid;
            Name = name;
            Elo = elo;
            WebSocket = webSocket;
        }
    }



    protected MultiplayerGame(string uuid, DateTime createdAt, DateTime updatedAt, GameBoard.Player? winner, GameBoard board, PlayerAccount playerX, PlayerAccount playerO, GameType type, GameState state) {
        UUID = uuid;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Winner = winner;
        Board = board;
        PlayerX = playerX;
        PlayerO = playerO;
        Type = type;
        State = state;
    }


    public string UUID { get; private set; }
    public ushort Round => Board.GetRound();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public GameBoard.Player? Winner { get; private set; }
    public string CurrentPlayer => Board.GetNextPlayer().ToString().ToUpper();
    public string NextPlayer => Board.GetCurrentPlayer().ToString().ToUpper();
    public PlayerAccount? PlayerX { get; private set; }
    public PlayerAccount? PlayerO { get; private set; }
    public GameType Type { get; private set; }
    public GameState State { get; private set; }

    [JsonIgnore]
    public GameBoard Board { get; private set; }

    [JsonInclude, JsonPropertyName("board")]
    private List<List<string>> json_Board => Board?.ToList() ?? new List<List<string>>();

    [JsonInclude, JsonPropertyName("winningCells")]
    private HashSet<List<int>>? json_WinningCells {
        get {
            if (Winner == null || Board == null) return null;

            return Board.GetWinningCells()?.Select(cell => new List<int> { cell.row, cell.col }).ToHashSet();
        }
    }




    /*public static async Task<List<MultiplayerGame>> GetAllAsync() {
        var list = new List<MultiplayerGame>();

        await using var conn = await Database.GetConnectionAsync();
        if(conn == null) return list;

        await using var cmd = new MySqlCommand("SELECT * FROM multiplayer_games", conn);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null) return list;

        while (await reader.ReadAsync()) {
            var game = new MultiplayerGame(
                reader.GetString("uuid"),
                reader.GetUInt16("round"),
                reader.GetDateTime("created_at"),
                reader.GetDateTime("updated_at"),
                reader.GetValueOrNull<string>("winner"),
                GameBoard.Parse(reader.GetValueOrNull<string>("board")!)
            );

            list.Add(game);
        }

        return list;
    }

    public static List<MultiplayerGame> GetAll() => GetAllAsync().Result;*/

    public static async Task<MultiplayerGame?> ReplaceCellAsync(string gameUUID, ushort x, ushort y, string? letter = null) {
        await using var conn = await Database.GetConnectionAsync();
        if(conn == null) return null;



        await using var cmd = new MySqlCommand(
        """
                    SELECT 
                        *, 
                        po.uuid AS player_o_uuid, 
                        po.display_name AS player_o_name, 
                        po.elo AS player_o_elo, 
                        px.uuid AS player_x_uuid, 
                        px.display_name AS player_x_name, 
                        px.elo AS player_x_elo
                    FROM multiplayer_games
                    JOIN users po ON multiplayer_games.player_o = po.uuid
                    JOIN users px ON multiplayer_games.player_x = px.uuid
                    WHERE multiplayer_games.uuid = @uuid;
                """, conn);

        cmd.Parameters.AddWithValue("@uuid", gameUUID);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null) return null;

        if(!await reader.ReadAsync()) return null;

        var playerX = new PlayerAccount(
            reader.GetValueOrNull<string>("player_x_uuid") ?? Guid.NewGuid().ToString(),
            reader.GetValueOrNull<string>("player_x_name") ?? "Guest",
            reader.GetValueOrNull<UInt32?>("player_x_elo") ?? 400
        );

        var playerO = new PlayerAccount(
            reader.GetValueOrNull<string>("player_o_uuid") ?? Guid.NewGuid().ToString(),
            reader.GetValueOrNull<string>("player_o_name") ?? "Guest",
            reader.GetValueOrNull<UInt32?>("player_o_elo") ?? 400
        );

        var game = new MultiplayerGame(
            reader.GetString("uuid"),
            reader.GetDateTime("created_at"),
            reader.GetDateTime("updated_at"),
            Enum.TryParse<GameBoard.Player>(reader.GetValueOrNull<string>("winner"), out var winner) ? winner : null,
            GameBoard.Parse(reader.GetString("board")),
            playerX,
            playerO,
            Enum.Parse<GameType>(reader.GetString("type")),
            Enum.Parse<GameState>(reader.GetString("state"))
        );

        await reader.CloseAsync();


        GameBoard.Player player = game.Board.GetCurrentPlayer() == GameBoard.Player.O ? GameBoard.Player.X : GameBoard.Player.O;
        game.Board.SetCell(x, y, player);

        game.Winner = game.Board.GetWinner();

        await using var cmd2 = new MySqlCommand("UPDATE multiplayer_games SET board = @board, winner = @winner WHERE uuid = @uuid", conn);
        cmd2.Parameters.AddWithValue("@uuid", gameUUID);
        cmd2.Parameters.AddWithValue("@board", game.Board.ToString());
        cmd2.Parameters.AddWithValue("@winner", winner.ToString().ToUpper());

        await cmd2.ExecuteNonQueryAsync();

        return game;
    }

    public static async Task<MultiplayerGame?> GetAsync(string uuid) {
        await using var conn = await Database.GetConnectionAsync();
        if(conn == null) return null;

        await using var cmd = new MySqlCommand(
            """
            SELECT 
                *, 
                po.uuid AS player_o_uuid, 
                po.display_name AS player_o_name, 
                po.elo AS player_o_elo, 
                px.uuid AS player_x_uuid, 
                px.display_name AS player_x_name, 
                px.elo AS player_x_elo
            FROM multiplayer_games
            JOIN users po ON multiplayer_games.player_o = po.uuid
            JOIN users px ON multiplayer_games.player_x = px.uuid
            WHERE multiplayer_games.uuid = @uuid
            """, conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null) return null;

        if(!await reader.ReadAsync()) return null;


        var playerX = new PlayerAccount(
            reader.GetValueOrNull<string>("player_x_uuid") ?? Guid.NewGuid().ToString(),
            reader.GetValueOrNull<string>("player_x_name") ?? "Guest",
            reader.GetValueOrNull<UInt32?>("player_x_elo") ?? 400
        );

        var playerO = new PlayerAccount(
            reader.GetValueOrNull<string>("player_o_uuid") ?? Guid.NewGuid().ToString(),
            reader.GetValueOrNull<string>("player_o_name") ?? "Guest",
            reader.GetValueOrNull<UInt32?>("player_o_elo") ?? 400
        );


        return new MultiplayerGame(
            reader.GetString("uuid"),
            reader.GetDateTime("created_at"),
            reader.GetDateTime("updated_at"),
            Enum.TryParse<GameBoard.Player>(reader.GetValueOrNull<string>("winner"), out var winner) ? winner : null,
            GameBoard.Parse(reader.GetString("board")),
            playerX,
            playerO,
            Enum.Parse<GameType>(reader.GetString("type")),
            Enum.Parse<GameState>(reader.GetString("state"))
        );
    }

    public static async Task<MultiplayerGame?> CreateAsync(PlayerAccount player1, PlayerAccount player2, GameType type) {
        await using var conn = await Database.GetConnectionAsync();
        if(conn == null) return null;

        var game = new MultiplayerGame(
            Guid.NewGuid().ToString(),
            DateTime.Now,
            DateTime.Now,
            null,
            GameBoard.CreateNew(),
            player1,
            player2,
            type,
            GameState.RUNNING
        );

        await using var cmd = new MySqlCommand("INSERT INTO multiplayer_games (uuid, round, created_at, updated_at, winner, board, player_x, player_o, type) VALUES (@uuid, @round, @created_at, @updated_at, @winner, @board, @player_x, @player_o, @type)", conn);
        cmd.Parameters.AddWithValue("@uuid", game.UUID);
        cmd.Parameters.AddWithValue("@round", game.Round);
        cmd.Parameters.AddWithValue("@created_at", game.CreatedAt);
        cmd.Parameters.AddWithValue("@updated_at", game.UpdatedAt);
        cmd.Parameters.AddWithValue("@winner", game.Winner);
        cmd.Parameters.AddWithValue("@board", game.Board.ToString());
        cmd.Parameters.AddWithValue("@player_x", player1.UUID);
        cmd.Parameters.AddWithValue("@player_o", player2.UUID);
        cmd.Parameters.AddWithValue("@type", type.ToString().ToUpper());

        var success = await cmd.ExecuteNonQueryAsync() > 0;
        return success ? game : null;
    }

    public static MultiplayerGame? Create(in PlayerAccount player1, in PlayerAccount player2, in GameType type) => CreateAsync(player1, player2, type).Result;

    public static async Task EndAsync(string gameUUID, PlayerAccount? winner) {
        await using var conn = await Database.GetConnectionAsync();
        if(conn == null) return;

        await using var cmd = new MySqlCommand("UPDATE multiplayer_games SET winner = @winner, state = 'FINISHED' WHERE uuid = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", gameUUID);
        cmd.Parameters.AddWithValue("@winner", winner?.UUID);

        var res = await cmd.ExecuteNonQueryAsync();
        //Console.WriteLine($"Game {gameUUID} ended with {(winner == null ? "draw" : winner.Name)}");
    }
}