using Microsoft.AspNetCore.Mvc;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Controllers;

public class UserController : Controller {

    [HttpGet("/user/{username}"), HttpGet("/@{username}")]
    public IActionResult DisplayUser(string username) {
        if (HCS.Current.Items["queryAccount"] is not Account queryAccount) {
            return new StatusCodeResult(404);
        }

        if (queryAccount.Username != username) {
            return new StatusCodeResult(404);
        }

        ViewBag.QueryAccount = queryAccount;
        return View("/Views/Account.cshtml");
    }
}