using TdA25_Error_Makers.Classes;

namespace TdA25_Error_Makers.Middlewares;



/*
 * Tento middleware kontroluje např. přihlášení 
 */
public class BeforeInitMiddleware(RequestDelegate next){
    public async Task InvokeAsync(HttpContext context) {
        
        // přihlášení
        var accTask = Auth.ReAuthUserAsync();

        await accTask;
        await next(context);
    }
}