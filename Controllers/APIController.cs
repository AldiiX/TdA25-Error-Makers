using Microsoft.AspNetCore.Mvc;
using TdA25_Error_Makers.Classes;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Controllers;



[ApiController]
[Route("api")]
public class APIController : Controller {

    public IActionResult Index() => new JsonResult(new { organization = "Student Cyber Games" });

    [HttpGet("app/restartmysql")]
    public IActionResult RestartMySQL() {
        var user = Auth.ReAuthUser();
        if(user is not { AccountType: Account.TypeOfAccount.ADMIN }) return new StatusCodeResult(403);

        Database.SwitchToNormalServer();
        using var conn = Database.GetConnection();
        if(conn == null) return new BadRequestObjectResult(new { success = false, message = "Nepodařilo se restartovat MySQL server" });


        return new OkObjectResult(new { success = true, message = $"Připojeno na { conn.DataSource }." });
    }
}