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



        // async věci
        var accTask = Auth.ReAuthUserAsync();
        //var gameTask = path.StartsWith("/game/") && path != "/game/" ? Game.GetByUUIDAsync(path.Split("/")[2]) : null;
        var createdGame = path is "/play/singleplayer/1v1" or "/play/singleplayer/1v1/" or "/play/singleplayer/1v1/game" or "/play/singleplayer/1v1/game/" or "/play/singleplayer/vsai" or "/play/singleplayer/vsai/" or "/play/singleplayer/vsai/game" or "/play/singleplayer/vsai/game/" ? Game.CreateAsync(null, Game.GameDifficulty.MEDIUM, GameBoard.CreateNew(), false, false, true) : null;


        // používání async věcí
        /*if (path.StartsWith("/game/") && path != "/game/") {
            Game? game = gameTask != null ? await gameTask : null;

            context.Items["game"] = game;
        }*/
        
        if(path is "/play/singleplayer/1v1" or "/play/singleplayer/1v1/" or "/play/singleplayer/1v1/game" or "/play/singleplayer/1v1/game/" or "/play/singleplayer/vsai" or "/play/singleplayer/vsai/" or "/play/singleplayer/vsai/game" or "/play/singleplayer/vsai/game/") {
            var game = createdGame != null ? await createdGame : null;
            if(game == null) {
                context.Response.StatusCode = 400;
                return;
            }

            context.Items["game"] = game;
        }



        // zbytek
        await accTask;
        await next(context);
    }
}