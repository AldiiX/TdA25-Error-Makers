using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class LeaderboardController : Controller {

    [HttpGet("/leaderboard")]
    public IActionResult Leaderboard() {
        return View("/Views/Leaderboard.cshtml");
    }
}