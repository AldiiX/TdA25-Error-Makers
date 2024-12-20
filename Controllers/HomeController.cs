using Microsoft.AspNetCore.Mvc;
namespace TdA25_Error_Makers.Controllers;

public class HomeController : Controller {
    [HttpGet("/")]
    public IActionResult Index() => View("/Views/Home.cshtml");

    [HttpGet("/home"),
     HttpGet("/home.html"),
     HttpGet("/home.php"), 
     HttpGet("/index"),
     HttpGet("index.html"),
     HttpGet("index.php")
    ]
    public IActionResult Home_Page() => Redirect("/");


    [HttpGet("/privacy/gdpr")]
    public IActionResult GDPR() => View("/Views/GDPR.cshtml");

    [HttpGet("/privacy/cookies")]
    public IActionResult Cookies() => View("/Views/Cookies.cshtml");
}