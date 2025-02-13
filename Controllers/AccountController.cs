using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Controllers;

public class AccountController : Controller {

    [Route("/account")]
    public IActionResult Account()
    {
        var user = Utilities.GetLoggedAccountFromContextOrNull();
        if (user == null) return Redirect("/login");
        return View("/Views/Account.cshtml");
    }

    [HttpGet("/ucet"), HttpGet("/acc"), HttpGet("/uzivatel")]
    public IActionResult Redirection() => Redirect("/account");

   
}

