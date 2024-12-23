using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Middlewares;



/*
 * Tento middleware kontroluje např. přihlášení 
 */
public class BeforeInitMiddleware(RequestDelegate next){
    public async Task InvokeAsync(HttpContext context) {
        string path = context.Request.Path.Value ?? "/";
        
        // přihlášení
        var accTask = Auth.ReAuthUserAsync();

        // specifické případy
        if (path.StartsWith("/game/") && path != "/game/") {
            var uuid = path.Split("/")[2];
            var game = Game.GetByUUID(uuid);
            if (game == null) {
                context.Response.StatusCode = 404;
                return;
            }

            context.Items["game"] = game;
        }

        await accTask;
        await next(context);
    }
}