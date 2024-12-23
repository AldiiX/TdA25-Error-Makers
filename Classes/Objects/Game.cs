﻿using System.Text.Json;
using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;

namespace TdA25_Error_Makers.Classes.Objects;





public class Game {

    // picovinky
    public enum GameDifficulty { BEGINNER, EASY, MEDIUM, HARD, EXTREME }
    public enum GameState { OPENING, MIDGAME, ENDGAME, FINISHED }



    // vlastnosti
    public string UUID { get; private set; }
    public string Name { get; private set; }
    public ushort Round { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsSaved { get; private set; }

    [JsonIgnore]
    public GameBoard Board { get; private set; }

    [JsonInclude, JsonPropertyName("board")]
    private List<List<string>> json_Board => Board.ToList();

    [JsonIgnore]
    public GameState State { get; private set; }

    [JsonInclude, JsonPropertyName("gameState")]
    private string json_State => State.ToString().ToLower();

    [JsonIgnore]
    public GameDifficulty Difficulty { get; private set; }

    [JsonInclude, JsonPropertyName("difficulty")]
    private string json_DifficultyLevel => Difficulty.ToString().ToLower();



    // constructory
    public Game(string uuid, string name, GameBoard board, GameDifficulty difficulty, DateTime createdAt, DateTime updatedAt, GameState state, ushort round, bool isSaved) {
        UUID = uuid;
        Name = name;
        Difficulty = difficulty;
        Board = board;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        State = state;
        Round = round;
        IsSaved = isSaved;
    }



    // static metody
    public override string ToString() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase});

    public static async Task<List<Game>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return [];

        var games = new List<Game>();
        await using var cmd = new MySqlCommand("SELECT * FROM `games` ORDER BY `updated_at` DESC", conn);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null) return games;

        while (await reader.ReadAsync()) {
            try {
                games.Add(
                    new Game(
                        reader.GetString("uuid"),
                        reader.GetString("name"),
                        GameBoard.Parse(reader.GetValueOrNull<string?>("board")),
                        Enum.Parse<GameDifficulty>(reader.GetString("difficulty")),
                        reader.GetDateTime("created_at"),
                        reader.GetDateTime("updated_at"),
                        Enum.Parse<GameState>(reader.GetString("game_state")),
                        reader.GetUInt16("round"),
                        reader.GetBoolean("saved")
                    )
                );
            } catch (Exception e) {
                Program.Logger.Log(LogLevel.Error, $"Failed to parse game {reader.GetString("uuid")} from database: " + e.Message);
            }
        }

        return games;
    }

    public static List<Game> GetAll() => GetAllAsync().Result;

    public static async Task<Game?> GetByUUIDAsync(string uuid) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        await using var cmd = new MySqlCommand("SELECT * FROM `games` WHERE `uuid` = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null || !await reader.ReadAsync()) return null;

        return new Game(
            reader.GetString("uuid"),
            reader.GetString("name"),
            GameBoard.Parse(reader.GetValueOrNull<string?>("board")),
            Enum.Parse<GameDifficulty>(reader.GetString("difficulty")),
            reader.GetDateTime("created_at"),
            reader.GetDateTime("updated_at"),
            Enum.Parse<GameState>(reader.GetString("game_state")),
            reader.GetUInt16("round"),
            reader.GetBoolean("saved")
        );
    }

    public static Game? GetByUUID(in string uuid) => GetByUUIDAsync(uuid).Result;

    public static Game? Create(string? name, GameDifficulty difficulty, GameBoard board, bool isSaved = false, bool insertToDatabase = false) {
        name ??= GenerateRandomGameName();

        // zpracování boardy
        if (!board.IsValid()) return null;
        GameState gmss = board.GetRound() + 1 > 6 ? GameState.MIDGAME : GameState.OPENING;
        if(board.CheckIfSomeoneCanWin() != null || board.CheckIfSomeoneWon() != null) gmss = GameState.ENDGAME;

        var game = new Game(
            Guid.NewGuid().ToString(),
            name,
            board,
            difficulty,
            DateTime.Now,
            DateTime.Now,
            gmss,
            board.GetRound(),
            false
        );




        using var conn = Database.GetConnection();
        if (conn == null) return null;

        using var cmd = new MySqlCommand("INSERT INTO `games` (`uuid`, `name`, `difficulty`, `board`, `created_at`, `updated_at`, `game_state`, `round`) VALUES (@uuid, @name, @difficulty, @board, @created_at, @updated_at, @game_state, @round)", conn);
        cmd.Parameters.AddWithValue("@uuid", game.UUID);
        cmd.Parameters.AddWithValue("@name", game.Name);
        cmd.Parameters.AddWithValue("@difficulty", game.Difficulty.ToString());
        cmd.Parameters.AddWithValue("@board", game.Board.ToString());
        cmd.Parameters.AddWithValue("@created_at", game.CreatedAt);
        cmd.Parameters.AddWithValue("@updated_at", game.UpdatedAt);
        cmd.Parameters.AddWithValue("@game_state", gmss.ToString());
        cmd.Parameters.AddWithValue("@round", board.GetRound());

        int res = 0;
        try {
            res = cmd.ExecuteNonQuery();
        } catch (Exception e) {
            Program.Logger.Log(LogLevel.Error, "Failed to insert new game to database: " + e.Message);
        }

        return res <= 0 ? null : game;
    }

    public static GameDifficulty ParseDifficulty(string difficulty) => !Enum.TryParse<GameDifficulty>(difficulty.ToUpper(), out var _diff) ? GameDifficulty.BEGINNER : _diff;

    public static string GenerateRandomGameName() {
        var adjectives = new HashSet<string>() {
            "Monstrózní", "Najetá", "Vyhratelná", "Nerealistická", "Nehratelná", "Legenda",
            "Pikantní", "Rychlá", "Epická", "Vymazlená", "Neuvěřitelná", "Nemožná", "Náročná",
            "Návyková", "Nebezpečná", "Záhadná", "Zničující", "Impozantní", "Výzva", "Strhující",
            "Nezapomenutelná", "Vítězná", "Porazitelná", "Neodolatelná", "Dramatická", "Taktická",
            "Rychlostní", "Energetická", "Složitá", "Vybalancovaná", "Historická", "Intenzivní",
            "Válečná", "Sci-fi", "Post-apokalyptická", "Moderní", "Pohádková", "Akční",
            "Simulátorová", "Logická", "Strategická", "Dobrodružná", "Plná", "Detailní",
            "Atmosférická", "Velkolepá", "Týmová", "Zábavná", "Fantastická", "Obrovská",
            "Vzrušující", "Smrtící", "Mistrovská", "Dynamická", "Precizní", "Pokročilá", "Pohodová",
            "Kreativní", "Chytlavá", "Unikátní", "Futuristická", "Realistická", "Výpravná",
            "Společenská", "Hluboká", "Technologická", "Nečekaná", "Riskantní", "Odpočinková",
            "Adrenalinová", "Tajemná", "Neprozkoumaná", "Objevná", "Silná", "Komplexní", "Minimalistická",
            "Inovativní", "Experimentální", "Retro-futuristická", "Nekonečná", "Skvělá", "Výrazná",
            "Inspirativní", "Autentická", "Inteligentní", "Propracovaná", "Bohatá", "Překvapivá",
            "Elegantní", "Přesná", "Vytrvalá", "Monumentální", "Nápaditá", "Ikonická", "Profesionální",
            "Přátelská", "Zrádná", "Kontroverzní", "Přizpůsobivá", "Spontánní", "Sympatická",
            "Vzácná", "Úžasná", "Kvalitní", "Dokonalá", "Osvěžující", "Trefná", "Nesmlouvavá",
            "Neústupná", "Zvláštní", "Legendární", "Globální", "Virtuální", "Zničující", "Neodolatelná",
            "Elegantní", "Tajemná", "Tématická", "Děsivá", "Magická", "Okouzlující", "Oslnivá",
            "Poutavá", "Dobrodružná", "Komediální", "Zásadní", "Nevídaná", "Náročná", "Neskutečná",
            "Představitelná", "Odhodlaná", "Pravdivá", "Hbitá", "Rychlá", "Neviditelná", "Blesková",
            "Silná", "Lidská", "Robotická", "Technická", "Rušná", "Veselá", "Osudová", "Progresivní",
            "Nezkrotná", "Nebývalá", "Nevšední", "Uvolněná", "Přímá", "Zásadní", "Transformační",
            "Odvážná", "Chytrá", "Zdrcující", "Hustá", "Spontánní", "Riskantní", "Vzrušující",
            "Hravá", "Vědecká", "Filozofická", "Zákeřná",
            "Ironická", "Nezávislá", "Pochmurná", "Romantická", "Rozkošná", "Nostalgická", "Hrdinská",
            "Přímočará", "Okázalá", "Rytmická", "Sofistikovaná", "Mistrovská", "Energetická",
            "Optimistická", "Pesimistická", "Komediální", "Prudká", "Neústupná", "Podmanivá",
            "Zničující", "Fascinující", "Hravá", "Vítězná", "Otevřená", "Zavřená", "Uzavřená",
            "Nesmrtelná", "Virtuální", "Hybridní", "Anonymní", "Odlišná", "Nenápadná", "Předvídatelná",
            "Nepředvídatelná", "Všestranná", "Jedinečná", "Překvapivá", "Nevysvětlitelná", "Zábavná",
            "Srdcervoucí", "Tragická", "Okamžitá", "Neznámá", "Podivná", "Zlověstná", "Pohádková",
            "Primitivní", "Podrobná", "Důležitá", "Globální", "Symbolická", "Neomezená", "Smrtelná",
            "Podvodná", "Vítězná", "Přizpůsobivá", "Neutrální", "Pokročilá", "Pokroková", "Starodávná",
            "Uvařená", "Navařená", "Upečená", "Napečená"
        };

        var adverbs = new HashSet<string>() {
            "Extrémně", "Realisticky", "Monstrózně", "Najetě", "Vyhratelně", "Herně",
            "Mega", "Nerealisticky", "Nehratelně", "Legendárně", "Fantasticky", "Obrovsky",
            "Pikantně", "Rychle", "Epicky", "Vymazleně", "Neuvěřitelně", "Nemožně", "Náročně",
            "Návykově", "Nebezpečně", "Záhadně", "Zničujícím způsobem", "Impozantně", "Výzvově", "Strhujícím způsobem",
            "Nezapomenutelně", "Vítězně", "Porazitelně", "Neodolatelně", "Dramaticky", "Takticky",
            "Rychlostně", "Energeticky", "Složitě", "Vybalancovaně", "Historicky", "Intenzivně",
            "Válečně", "Sci-fi", "Post-apokalypticky", "Retro", "Moderně", "Pohádkově", "Akčně",
            "RPG", "Simulátorově", "Logicky", "Strategicky", "Dobrodružně", "Plně", "Detailně",
            "Atmosféricky", "Velkolepě", "Týmově", "Zábavně", "Vzrušujícím způsobem", "Smrtelně", "Mistrovsky",
            "Dynamicky", "Precizně", "Pokročile", "Pohodově", "Kreativně", "Chytlavě", "Unikátně", "Futuristicky",
            "Dynamicky", "Precizně", "Pokročile", "Pohodově", "Kreativně", "Chytlavě", "Unikátně", "Futuristicky",
            "Realisticky", "Výpravně", "Společensky", "Hluboce", "Technologicky", "Nečekaně", "Riskantně",
            "Odpočinkově", "Adrenalinově", "Tajemně", "Neprozkoumaně", "Objevně", "Silně", "Komplexně",
            "Minimalisticky", "Inovativně", "Experimentálně", "Retro-futuristicky", "Nekonečně", "Skvěle",
            "Výrazně", "Inspirativně", "Autenticky", "Inteligentně", "Propracovaně", "Bohatě", "Překvapivě",
            "Elegantně", "Přesně", "Vytrvale", "Monumentálně", "Nápaditě", "Ikonicky", "Profesionálně",
            "Přátelsky", "Zrádně", "Kontroverzně", "Přizpůsobivě", "Spontánně", "Sympaticky",
            "Vzácně", "Úžasně", "Kvalitně", "Dokonale", "Osvěžujícím způsobem", "Trefně", "Nesmlouvavě",
            "Neústupně", "Zvláštně", "Legendárně", "Globálně", "Virtuálně", "Zničujícím způsobem",
            "Neodolatelně", "Elegantně", "Tajemně", "Tématicky", "Děsivě", "Magicky", "Okouzlujícím způsobem",
            "Oslnivě", "Poutavě", "Dobrodružně", "Komediálně", "Zásadně", "Nevídaně", "Náročně", "Neskutečně",
            "Představitelně", "Odhodlaně", "Pravdivě", "Hbitě", "Rychle", "Neviditelně", "Bleskově", "Silně",
            "Lidsky", "Roboticky", "Technicky", "Rušně", "Vesele", "Osudově", "Progresivně",
            "Nezkrotně", "Nebývale", "Nevšedně", "Uvolněně", "Přímo", "Zásadně", "Transformačně",
            "Odvážně", "Chytře", "Zdrcujícím způsobem", "Hustě", "Spontánně", "Riskantně", "Vzrušujícím způsobem",
            "Hravě", "Vědecky", "Filozoficky", "Zákeřně", "Ironicky", "Nezávisle", "Pochmurně", "Romanticky",
            "Rozkošně", "Nostalgicky", "Hrdinsky", "Přímo", "Okázale", "Rytmicky", "Sofistikovaně", "Mistrovsky",
            "Energeticky", "Optimisticky", "Pesimisticky", "Komediálně", "Prudce", "Neústupně",
            "Podmanivě", "Zničujícím způsobem", "Fascinujícím způsobem", "Hravě", "Vítězně", "Otevřeně",
            "Zavřeně", "Uzavřeně", "Nesmrtelně", "Virtuálně", "Hybridně", "Anonymně", "Odlišně",
            "Nenápadně", "Předvídatelně", "Nepředvídatelně", "Všestranně", "Jedinečně", "Překvapivě",
            "Nevysvětlitelně", "Zábavně", "Srdcervoucím způsobem", "Tragicky", "Okamžitě", "Neznámě",
            "Podivně", "Zlověstně", "Pohádkově", "Primitivně", "Podrobně", "Důležitě", "Globálně",
            "Symbolicky", "Neomezeně", "Smrtelně", "Podvodně", "Vítězně", "Přizpůsobivě", "Neutrálně",
            "Pokročile", "Pokrokově", "Starodávně", "Uvařeně", "Navařeně", "Upečeně", "Napečeně", "Zkrátka",
            "Naprosto", "Prostě"
        };


        string adverb = adjectives.ElementAt(new Random().Next(adjectives.Count));
        string adjective = adverbs.ElementAt(new Random().Next(adverbs.Count));

        return $"{adjective} {adverb} Hra";
    }
}