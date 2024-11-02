using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;



[ApiController]
[Route("api")]
public class APIController : Controller {

    public IActionResult Index() => new JsonResult(new { organization = "Student Cyber Games" });
}