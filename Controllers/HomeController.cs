using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class HomeController : Controller {


    [Route("/")]
    public IActionResult Index() => Hello();

    [Route("/hello")]
    public IActionResult Hello() => View("/Views/Hello.cshtml");
}