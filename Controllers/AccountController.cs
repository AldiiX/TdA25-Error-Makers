using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class AccountController : Controller {
    

    [Route("/account")]
    public IActionResult Account() => View("/Views/Account.cshtml");
    
    [HttpGet("/ucet"), HttpGet("/acc"), HttpGet("/uzivatel")]
    public IActionResult Redirection() => Redirect("/account");
}

