using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

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
}