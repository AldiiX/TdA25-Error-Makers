using System.Text.Json;

namespace TdA25_Error_Makers.Classes;

public class LowerCaseNamingPolicy : JsonNamingPolicy {
    public override string ConvertName(string name) => name.ToLowerInvariant();
}