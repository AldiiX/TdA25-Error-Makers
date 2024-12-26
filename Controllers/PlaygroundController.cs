using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;


#if DEBUG || TESTING
[Route("playground")]
#endif
public class PlaygroundController : Controller {

    #if DEBUG || TESTING

    [HttpGet("home")]
    public IActionResult HomePage() {
        return View("/Views/Playground/Home.cshtml");
    }

    [HttpGet("error/{code:int}")]
    public IActionResult SendError(int code) => StatusCode(code);

    #endif
}