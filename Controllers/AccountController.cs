using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class AccountController : Controller {

    [HttpGet("/account")]
    public IActionResult Games() => View("/Views/Account.cshtml");

    [HttpGet("/ucet")]
    public IActionResult Redirection() => Redirect("/games");
}