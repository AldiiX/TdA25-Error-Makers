using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class GameController : Controller {

    [HttpGet("/game")]
    public IActionResult Game() {
        return View("/Views/Game.cshtml");
    }

    [HttpGet("/game/{uuid}")]
    public IActionResult Game(string uuid) {
        var game = Classes.Objects.Game.GetByUUID(uuid);
        if(game == null) return NotFound();

        ViewBag.Game = game;
        return View("/Views/Game.cshtml");
    }
}