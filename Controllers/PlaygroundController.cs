using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;


[Route("playground")]
public class PlaygroundController : Controller {

    [HttpGet("home")]
    public IActionResult HomePage() {
        return View("/Views/Playground/Home.cshtml");
    }
}