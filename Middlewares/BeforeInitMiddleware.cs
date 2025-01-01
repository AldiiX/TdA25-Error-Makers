using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Middlewares;



/*
 * Tento middleware kontroluje např. přihlášení 
 */
public class BeforeInitMiddleware(RequestDelegate next){
    public async Task InvokeAsync(HttpContext context) {
        string path = context.Request.Path.Value ?? "/";



        // věci před jakýkoliv requestem
        if (path.StartsWith("/api/v2/")) {
            await next(context);
            return;
        }



        // přihlášení
        var accTask = Auth.ReAuthUserAsync();

        // specifické případy
        if (path.StartsWith("/game/") && path != "/game/") {
            var uuid = path.Split("/")[2];
            Game? game = Game.GetByUUID(uuid);

            context.Items["game"] = game;
        }

        await accTask;
        await next(context);
    }
}