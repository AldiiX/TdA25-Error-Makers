using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class HelloController : Controller {
    

    [Route("/hello")]
    public IActionResult Hello() => View("/Views/Hello.cshtml");
}