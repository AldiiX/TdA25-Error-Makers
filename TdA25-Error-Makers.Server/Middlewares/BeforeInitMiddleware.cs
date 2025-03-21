namespace TdA25_Error_Makers.Server.Middlewares;

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


        // vyhodnoceni
        /*if(path is "/play/singleplayer/1v1" or "/play/singleplayer/1v1/" or "/play/singleplayer/1v1/game" or "/play/singleplayer/1v1/game/" or "/play/singleplayer/vsai" or "/play/singleplayer/vsai/" or "/play/singleplayer/vsai/game" or "/play/singleplayer/vsai/game/") {
            var game = createdGame != null ? await createdGame : null;
            if(game == null) {
                context.Response.StatusCode = 400;
                return;
            }

            context.Items["game"] = game;
        }*/




        // zbytek
        await next(context);
    }
}