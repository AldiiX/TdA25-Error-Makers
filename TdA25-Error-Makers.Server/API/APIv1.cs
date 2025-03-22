using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using TdA25_Error_Makers.Server.Classes;

namespace TdA25_Error_Makers.Server.API;

[Route("api/v1")]
public class APIv1 : Controller {

    [HttpGet]
    public IActionResult Index() {
        return new JsonResult(new { success = true, message = "Welcome to the API" });
    }

    [HttpGet("projects")]
    public IActionResult GetProjects() {
        var array = new JsonArray();
        array.Add(new {
            title = "Project 1",
            description = "This is the first project"
        });
        array.Add(new {
            title = "Project 2",
            description = "This is the second project"
        });

        return new JsonResult(array);
    }

    [HttpGet("loggeduser")]
    public IActionResult GetLoggedUser() {
        var user = Utilities.GetLoggedAccountFromContextOrNull();
        if(user == null) return new JsonResult(new { success = false, message = "User is not logged in" });

        var obj = new JsonObject {
            { "name", user.Username },
            { "password", user.Password }
        };

        return new JsonResult(obj);
    }

    [HttpPost("loggeduser")]
    public IActionResult LogUser([FromBody] JsonObject obj) {
        var username = obj["username"]?.ToString();
        var password = obj["password"]?.ToString();

        if(username == null || password == null) return new BadRequestObjectResult(new { success = false, message = "Invalid request" });

        var acc = Auth.AuthUser(username, Utilities.EncryptPassword(password));
        if(acc == null) return new JsonResult(new { success = false, message = "Invalid credentials" });

        return new JsonResult(acc);
    }
}