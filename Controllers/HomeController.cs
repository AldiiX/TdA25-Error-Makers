using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class HomeController : Controller {


    [Route("/")]
    public IActionResult Index() {
        return View("/Views/Index.cshtml");
    }
}