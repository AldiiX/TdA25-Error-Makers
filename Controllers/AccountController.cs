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
    
    
    [HttpPost("/account")]
    public IActionResult Account_Post() {
        var user = Utilities.GetLoggedAccountFromContextOrNull();
        using var conn = Database.GetConnection();
        using var cmd = conn?.CreateCommand();
        cmd.CommandText =
            "DELETE FROM `users` WHERE `uuid` = @uuid";
        cmd.Parameters.AddWithValue("@uuid", user.UUID);
        var result = cmd.ExecuteNonQuery();
        
        return Redirect("/login");
    }
}

