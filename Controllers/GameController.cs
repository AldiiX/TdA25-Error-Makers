using Microsoft.AspNetCore.Mvc;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Controllers;





[Controller]
public class GameController : Controller {

    [HttpGet("/game")]
    public IActionResult GameCreate() {
        var g = Game.Create(null, Game.GameDifficulty.MEDIUM, GameBoard.CreateNew(), false, true);
        if(g == null) return BadRequest();

        return Redirect($"/game/{g.UUID}");
    }

    [HttpGet("/game/{uuid}")]
    public IActionResult GameSpecific() {
        if (HCS.Current.Items["game"] is not Game game) {
            ViewBag.ErrorCode = 404;
            ViewBag.ErrorMessage = "Hra s tímto UUID nebyla nalezena";
            ViewBag.ErrorMessage2 = "Zkus jiné UUID ¯\\_(ツ)_/¯";
            ViewBag.ButtonLink = "/games";
            return View("/Views/Error.cshtml");
        }

        ViewBag.Game = game;
        return View("/Views/Game.cshtml");
    }
}