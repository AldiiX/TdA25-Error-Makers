using System.Text.Json.Serialization;

namespace TdA25_Error_Makers.Classes;



public class LowerCaseJsonStringEnumConverter(): JsonStringEnumConverter(new LowerCaseNamingPolicy(), allowIntegerValues: true);
