using Microsoft.AspNetCore.Mvc;

namespace TdA25_Error_Makers.Controllers;

public class MenuController : Controller {
    
    [Route("/menu")]
    public IActionResult Menu() => View("/Views/Menu.cshtml");
    
}