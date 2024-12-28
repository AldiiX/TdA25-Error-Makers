using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class ErrorController: Controller {

    [HttpGet("error")]
    public IActionResult Render([FromQuery] int? code, [FromQuery] string? message, [FromQuery] string? message2, [FromQuery] string? buttonLink) {
        ViewBag.ErrorCode = code!;
        ViewBag.ErrorMessage = message!;
        ViewBag.ErrorMessage2 = message2!;
        ViewBag.ButtonLink = buttonLink!;
        return View("/Views/Error.cshtml");
    }
}