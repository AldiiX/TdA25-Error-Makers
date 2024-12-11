using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class HomeController : Controller {


    [Route("/")]
    public IActionResult Index() => View("/Views/Index.cshtml");

    [Route("/hello")]
    public IActionResult Hello() => View("/Views/Hello.cshtml");
}