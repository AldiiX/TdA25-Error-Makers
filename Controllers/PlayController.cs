using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;


[Route("play")]
public class PlayController : Controller{

    [HttpGet]
    public IActionResult Index(){
        return View("~/Views/Play.cshtml");
    }
}