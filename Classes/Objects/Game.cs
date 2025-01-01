using System.Text.Json;
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
    public string? Winner { get; private set; }
    public string CurrentPlayer => Board.GetNextPlayer().ToString().ToUpper();
    public bool IsInstance { get; private set; }

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

    [JsonInclude, JsonPropertyName("winningCells")]
    private HashSet<List<int>>? json_WinningCells {
        get {
            if (Winner == null) return null;

            return Board.GetWinningCells()?.Select(cell => new List<int> { cell.row, cell.col }).ToHashSet();
        }
    }



    // constructory
    public Game(string uuid, string name, GameBoard board, GameDifficulty difficulty, DateTime createdAt, DateTime updatedAt, GameState state, ushort round, bool isSaved, bool isInstance) {
        UUID = uuid;
        Name = name;
        Difficulty = difficulty;
        Board = board;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        State = state;
        Round = round;
        IsSaved = isSaved;
        Winner = board.GetWinner() != null ? board.GetWinner().ToString()?.ToUpper() : null;
        IsInstance = isInstance;
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
                        reader.GetBoolean("saved"),
                        reader.GetBoolean("is_instance")
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
            reader.GetBoolean("saved"),
            reader.GetBoolean("is_instance")
        );
    }

    public static Game? GetByUUID(in string uuid) => GetByUUIDAsync(uuid).Result;

    public static Game? Create(string? name, GameDifficulty difficulty, GameBoard board, bool isSaved = false, bool isInstance = false, bool insertToDatabase = false) {
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
            isSaved,
            isInstance
        );




        using var conn = Database.GetConnection();
        if (conn == null) return null;

        using var cmd = new MySqlCommand("INSERT INTO `games` (`uuid`, `name`, `difficulty`, `board`, `created_at`, `updated_at`, `game_state`, `round`, `saved`, `is_instance`) VALUES (@uuid, @name, @difficulty, @board, @created_at, @updated_at, @game_state, @round, @saved, @is_instance)", conn);
        cmd.Parameters.AddWithValue("@uuid", game.UUID);
        cmd.Parameters.AddWithValue("@name", game.Name);
        cmd.Parameters.AddWithValue("@difficulty", game.Difficulty.ToString());
        cmd.Parameters.AddWithValue("@board", game.Board.ToString());
        cmd.Parameters.AddWithValue("@created_at", game.CreatedAt);
        cmd.Parameters.AddWithValue("@updated_at", game.UpdatedAt);
        cmd.Parameters.AddWithValue("@game_state", gmss.ToString());
        cmd.Parameters.AddWithValue("@round", board.GetRound());
        cmd.Parameters.AddWithValue("@saved", isSaved);
        cmd.Parameters.AddWithValue("@is_instance", isInstance);

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
            "Monstrózní", "Najetá", "Vyhratelná", "Nerealistická", "Nehratelná",
            "Pikantní", "Rychlá", "Epická", "Vymazlená", "Neuvěřitelná", "Nemožná", "Náročná",
            "Návyková", "Nebezpečná", "Záhadná", "Zničující", "Impozantní", "Vyzývavá", "Strhující",
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
            "Neústupná", "Zvláštní", "Legendární", "Globální", "Virtuální", "Tématická", "Děsivá",
            "Magická", "Okouzlující", "Oslnivá", "Poutavá", "Komediální", "Nevídaná", "Neskutečná",
            "Představitelná", "Odhodlaná", "Pravdivá", "Hbitá", "Neviditelná", "Blesková",
            "Lidská", "Robotická", "Technická", "Rušná", "Veselá", "Osudová", "Progresivní",
            "Nezkrotná", "Nebývalá", "Nevšední", "Uvolněná", "Přímá", "Transformační",
            "Odvážná", "Chytrá", "Zdrcující", "Hustá", "Vědecká", "Filozofická", "Zákeřná",
            "Ironická", "Nezávislá", "Pochmurná", "Romantická", "Rozkošná", "Nostalgická", "Hrdinská",
            "Rytmická", "Sofistikovaná", "Energetická", "Optimistická", "Pesimistická", "Prudká",
            "Podmanivá", "Fascinující", "Otevřená", "Zavřená", "Uzavřená", "Nesmrtelná",
            "Hybridní", "Anonymní", "Odlišná", "Nenápadná", "Předvídatelná", "Nepředvídatelná",
            "Všestranná", "Jedinečná", "Nevysvětlitelná", "Srdcervoucí", "Tragická", "Okamžitá",
            "Neznámá", "Podivná", "Zlověstná", "Primitivní", "Podrobná", "Důležitá",
            "Symbolická", "Neomezená", "Smrtelná", "Podvodná", "Neutrální", "Pokroková",
            "Starodávná", "Významná", "Evoluční", "Průkopnická", "Neúnavná", "Neotřelá",
            "Charismatická", "Elektrizující", "Prorocká", "Smysluplná",
            "Rychlopalná", "Spolupracující", "Stěžejní", "Útočná", "Defenzivní", "Rozhodující",
            "Hraniční", "Dlouhodobá", "Dočasná", "Úderná", "Plánovací", "Vynalézavá",
            "Pozoruhodná", "Nezvratná", "Flexibilní", "Souvislá", "Hladká", "Kritická",
            "Neokázalá", "Vlivná", "Nevyjasněná", "Výstražná", "Rezonující", "Udržitelná"
        };

        var adverbs = new HashSet<string>() {
            "Extrémně", "Realisticky", "Monstrózně", "Najetě", "Vyhratelně", "Herně",
            "Mega", "Nerealisticky", "Nehratelně", "Legendárně", "Fantasticky", "Obrovsky",
            "Pikantně", "Rychle", "Epicky", "Vymazleně", "Neuvěřitelně", "Nemožně", "Náročně",
            "Návykově", "Nebezpečně", "Záhadně", "Zničujícím způsobem", "Impozantně", "Výzvově",
            "Strhujícím způsobem", "Nezapomenutelně", "Vítězně", "Porazitelně", "Neodolatelně",
            "Dramaticky", "Takticky", "Rychlostně", "Energeticky", "Složitě", "Vybalancovaně",
            "Historicky", "Intenzivně", "Válečně", "Sci-fi", "Post-apokalypticky", "Retro",
            "Moderně", "Pohádkově", "Akčně", "Simulátorově", "Logicky", "Strategicky",
            "Dobrodružně", "Plně", "Detailně", "Atmosféricky", "Velkolepě", "Týmově", "Zábavně",
            "Vzrušujícím způsobem", "Smrtelně", "Mistrovsky", "Dynamicky", "Precizně", "Pokročile",
            "Pohodově", "Kreativně", "Chytlavě", "Unikátně", "Futuristicky", "Výpravně", "Společensky",
            "Hluboce", "Technologicky", "Nečekaně", "Riskantně", "Odpočinkově", "Adrenalinově",
            "Tajemně", "Neprozkoumaně", "Objevně", "Silně", "Komplexně", "Minimalisticky",
            "Inovativně", "Experimentálně", "Retro-futuristicky", "Nekonečně", "Skvěle",
            "Výrazně", "Inspirativně", "Autenticky", "Inteligentně", "Propracovaně", "Bohatě",
            "Překvapivě", "Elegantně", "Přesně", "Vytrvale", "Monumentálně", "Nápaditě",
            "Ikonicky", "Profesionálně", "Přátelsky", "Zrádně", "Kontroverzně", "Přizpůsobivě",
            "Spontánně", "Sympaticky", "Vzácně", "Úžasně", "Kvalitně", "Dokonale",
            "Osvěžujícím způsobem", "Trefně", "Nesmlouvavě", "Neústupně", "Zvláštně", "Globálně",
            "Virtuálně", "Tématicky", "Děsivě", "Magicky", "Okouzlujícím způsobem", "Oslnivě",
            "Poutavě", "Komediálně", "Nevídaně", "Náročně", "Neskutečně", "Představitelně",
            "Odhodlaně", "Pravdivě", "Hbitě", "Neviditelně", "Bleskově", "Lidsky", "Roboticky",
            "Technicky", "Rušně", "Vesele", "Osudově", "Progresivně", "Nezkrotně", "Nebývale",
            "Nevšedně", "Uvolněně", "Přímo", "Transformačně", "Odvážně", "Chytře",
            "Zdrcujícím způsobem", "Hustě", "Hravě", "Vědecky", "Filozoficky", "Zákeřně",
            "Ironicky", "Nezávisle", "Pochmurně", "Romanticky", "Rozkošně", "Nostalgicky",
            "Hrdinsky", "Rytmicky", "Sofistikovaně", "Energeticky", "Optimisticky", "Pesimisticky",
            "Prudce", "Podmanivě", "Fascinujícím způsobem", "Hravě", "Vítězně", "Otevřeně",
            "Hybridně", "Anonymně", "Odlišně", "Nenápadně", "Předvídatelně", "Nepředvídatelně",
            "Všestranně", "Jedinečně", "Nevysvětlitelně", "Srdcervoucím způsobem", "Tragicky",
            "Okamžitě", "Neznámě", "Podivně", "Zlověstně", "Primitivně", "Podrobně",
            "Důležitě", "Symbolicky", "Neomezeně", "Smrtelně", "Podvodně", "Neutrálně",
            "Pokrokově", "Starodávně", "Významně", "Evolučně", "Komplexně", "Průkopnicky",
            "Neúnavně", "Neotřele", "Charismaticky", "Elektrizujícím způsobem", "Prorocky",
            "Smysluplně", "Rychlopalně", "Spolupracujícím způsobem", "Stěžejně", "Útočně",
            "Defenzivně", "Rozhodujícím způsobem", "Hraničně", "Dlouhodobě", "Úderně",
            "Plánovacím způsobem", "Vynalézavě", "Pozoruhodně", "Nezvratně", "Flexibilně",
            "Souvisle", "Hladce", "Kriticky", "Neokázale", "Vlivně", "Výstražně",
            "Rezonujícím způsobem", "Udržitelně"
        };


        string adverb = adjectives.ElementAt(new Random().Next(adjectives.Count));
        string adjective = adverbs.ElementAt(new Random().Next(adverbs.Count));

        return $"{adjective} {adverb} Hra";
    }
}