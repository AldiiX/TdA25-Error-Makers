using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class GameController : Controller {

    [HttpGet("/game")]
    public IActionResult Game() {
        return View("/Views/Game.cshtml");
    }
}