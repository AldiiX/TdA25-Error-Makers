global using HCS = TdA25_Error_Makers.Server.Services.HttpContextService;
namespace TdA25_Error_Makers.Server.Services;

public static class HttpContextService {
    private static IHttpContextAccessor _httpContextAccessor = null!;

    public static void Configure(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor;
    }

    public static HttpContext Current => _httpContextAccessor.HttpContext!;
}