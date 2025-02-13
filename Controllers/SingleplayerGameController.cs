using Microsoft.AspNetCore.Mvc;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Controllers;





[Controller, Route("play/singleplayer")]
public class SingleplayerGameController : Controller {

    [HttpGet("1v1/game"), HttpGet("1v1")]
    public IActionResult GameCreate() {
        if (HCS.Current.Items["game"] is not Game game) { // inicializuje se v BeforeInitMiddlewaru
            ViewBag.ErrorCode = 404;
            ViewBag.ErrorMessage = "Hra s tímto UUID nebyla nalezena";
            ViewBag.ErrorMessage2 = "Zkus jiné UUID ¯\\_(ツ)_/¯";
            ViewBag.ButtonLink = "/games";
            return View("/Views/Error.cshtml");
        }

        ViewBag.Game = game;
        Utilities.Cookie.Set("gameuuid", game.UUID);
        return View("/Views/Game.cshtml");
    }


    [HttpGet("vsai/game"), HttpGet("vsai")]
    public IActionResult CreateVsAIGame() {
        return RedirectPermanent("/play");
    }

    /*[HttpGet("/game/{uuid}")]
    public IActionResult GameSpecific(string uuid) {
        if (HCS.Current.Items["game"] is not Game game || game.UUID != uuid) {
            ViewBag.ErrorCode = 404;
            ViewBag.ErrorMessage = "Hra s tímto UUID nebyla nalezena";
            ViewBag.ErrorMessage2 = "Zkus jiné UUID ¯\\_(ツ)_/¯";
            ViewBag.ButtonLink = "/games";
            return View("/Views/Error.cshtml");
        }

        ViewBag.Game = game;
        Utilities.Cookie.Set("gameuuid", game.UUID);
        return View("/Views/Game.cshtml");
    }*/
}