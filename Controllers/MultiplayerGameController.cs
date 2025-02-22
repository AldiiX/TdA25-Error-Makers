using Microsoft.AspNetCore.Mvc;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Controllers;


[Route("multiplayer")]
public class MultiplayerGameController : Controller {

    [HttpGet("{uuid}")]
    public IActionResult Index(string uuid) {
        if (HCS.Current.Items["game"] is not MultiplayerGame game) return NotFound();
        if(game.PlayerX == null || game.PlayerO == null) return NotFound();

        // pokud hra skoncila
        if (game.State == MultiplayerGame.GameState.FINISHED) {
            ViewBag.ErrorCode = 404;
            ViewBag.ErrorMessage = "Hra již skončila";
            ViewBag.ButtonLink = "/play";
            ViewBag.ButtonText = "Nová hra";
            return View("~/Views/Error.cshtml");
        }

        var account = Utilities.GetLoggedAccountFromContextOrNull() ?? Account.GetByUUID(HCS.Current.Session.GetString("tempAccountUUID") ?? "0");
        if(account?.UUID != game.PlayerX.UUID && account?.UUID != game.PlayerO.UUID) {
            ViewBag.ErrorCode = 401;
            ViewBag.ErrorMessage = "Nemáš přístup k této hře";
            ViewBag.ErrorMessage2 = "Hru zkrátka hrají jiní hráči :)";
            ViewBag.ButtonLink = "/play";
            ViewBag.ButtonText = "Nová hra";
            return View("~/Views/Error.cshtml");
        }


        ViewBag.Game = game;
        return View("~/Views/MultiplayerGame.cshtml");
    }
}