using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using TdA25_Error_Makers.Services;

namespace TdA25_Error_Makers.Middlewares;





public class ErrorHandlingMiddleware(RequestDelegate next, IViewRenderService vrs) {

    public async Task InvokeAsync(HttpContext context) {
        // Nech middleware pokračovat
        await next(context);

        string path = context.Request.Path.Value ?? "/";

        // Pokud je odpověď chybová a ještě nebyla odeslána
        if (context.Response is { HasStarted: false, StatusCode: >= 400 and < 600 }) {
            string errorReasonPhrase = context.Response.StatusCode switch {
                404 => $"Stránka „{path}” nebyla nalezena",
                403 => "K tomuto obsahu nemáš přístup",
                500 => "Něco na serveru se pokazilo, zkuste to prosím později",
                _ => ReasonPhrases.GetReasonPhrase(context.Response.StatusCode) != "" ? ReasonPhrases.GetReasonPhrase(context.Response.StatusCode) : "Něco se pokazilo",
            };

            if (path.StartsWith("/api") || path.StartsWith("/iapi")) {
                // API chyba: JSON odpověď
                var errorResponse = new {
                    success = false,
                    code = context.Response.StatusCode,
                    message = errorReasonPhrase
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = context.Response.StatusCode; // Zajistí správný status kód
                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                return;
            }

            // Webová chyba: Renderuje stránku Views/Error.cshtml
            context.Response.ContentType = "text/html";
            var renderedView = await vrs.RenderViewToStringAsync("Error", null, new { ErrorCode = context.Response.StatusCode, ErrorMessage = errorReasonPhrase });

            await context.Response.WriteAsync(renderedView);
        }
    }
}