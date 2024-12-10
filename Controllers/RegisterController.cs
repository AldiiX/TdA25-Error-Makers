namespace TdA25_Error_Makers.Controllers;
using Microsoft.AspNetCore.Mvc;

public class RegisterController : Controller {
    [Route("/register")]
    public IActionResult Register_Page() => View("/Views/Register.cshtml");
    
    [Route("/register")]
    [HttpPost]
    public IActionResult Register_Post(string username, string password) {
        // This is where you would create a new user with the given
        // username and password.
        return Redirect("/login");
    }
}