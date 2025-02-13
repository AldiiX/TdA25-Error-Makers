using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class GamesController : Controller {

    /*[HttpGet("/games")]
    public IActionResult Games() => View("/Views/Games.cshtml");

    [HttpGet("/hry"), HttpGet("/ulohy")]
    public IActionResult Redirection() => Redirect("/games");*/

    [HttpGet("/hry"), HttpGet("/ulohy"), HttpGet("/games")]
    public IActionResult R() => Redirect("/");
}