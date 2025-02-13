using Microsoft.AspNetCore.Mvc;
using TdA25_Error_Makers.Classes;

namespace TdA25_Error_Makers.Controllers;


[Route("multiplayer")]
public class MultiplayerController : Controller {

    [HttpGet]
    public IActionResult Index() {
        return View("~/Views/Multiplayer.cshtml");
    }
}