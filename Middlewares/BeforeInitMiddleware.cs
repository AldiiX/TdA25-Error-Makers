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
        
        else if(path is "/game" or "/game/") {
            var game = Game.Create(null, Game.GameDifficulty.MEDIUM, GameBoard.CreateNew(), false, false, true);
            if(game == null) {
                context.Response.StatusCode = 400;
                return;
            }

            context.Items["game"] = game;
        }

        await accTask;
        await next(context);
    }
}