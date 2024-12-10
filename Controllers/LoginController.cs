namespace TdA25_Error_Makers.Controllers;
using Microsoft.AspNetCore.Mvc;

public class LoginController : Controller {
    [Route("/login")]
    public IActionResult Login_Page() => View("/Views/Login.cshtml");
    
    [Route("/login")]
    [HttpPost]
    public IActionResult Login_Post(string username, string password) {
        // This is where you would check the user's credentials
        // and log them in if they are correct.
        return Redirect("/");
    }
    
}