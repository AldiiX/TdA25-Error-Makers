using TdA25_Error_Makers.Classes;

namespace TdA25_Error_Makers.Controllers;
using Microsoft.AspNetCore.Mvc;

public class AuthController : Controller {
    [Route("/login")]
    public IActionResult Login_Page()
    {
        ViewBag.AuthType = "login";
        return View("/Views/Auth.cshtml");
    } 
    
    [HttpPost("/login")]
    public IActionResult Login_Post([FromForm] string username, [FromForm] string password) {
        ViewBag.AuthType = "login";
        var acc = Auth.AuthUser(username, Utilities.EncryptPassword(password));
        if (acc == null) {
            ViewBag.ErrorMessage = "Uživatel neexistuje.";
            return View("/Views/Auth.cshtml");
        }

        return Redirect("/");
    }

    [Route("/logout")]
    public IActionResult Logout() {
        HCS.Current.Session.Remove("loggeduser");
        return Redirect("/");
    }

    [Route("/register")]
    public IActionResult Register_Page()
    {
        ViewBag.AuthType = "register";
        return View("/Views/Auth.cshtml");
    }

    [HttpPost("/register")]
    public IActionResult Register_Post([FromForm] string username, [FromForm] string email, [FromForm] string password, [FromForm] string passwordConfirm) {
        ViewBag.AuthType = "register";

        // validace dat
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordConfirm)) {
            ViewBag.ErrorMessage = "Všechna pole musí být vyplněna.";
            return View("/Views/Auth.cshtml");
        }

        if (password != passwordConfirm) {
            ViewBag.ErrorMessage = "Hesla se neshodují.";
            return View("/Views/Auth.cshtml");
        }

        using var checkUsernameCmd = Database.GetConnection().CreateCommand();
        checkUsernameCmd.CommandText = "SELECT COUNT(*) FROM `users` WHERE `username` = @username";
        checkUsernameCmd.Parameters.AddWithValue("@username", username);
        var usernameExists = Convert.ToInt32(checkUsernameCmd.ExecuteScalar()) > 0;

        if (usernameExists) {
            ViewBag.ErrorMessage = "Uživatelské jméno už je zabrané.";
            return View("/Views/Auth.cshtml");
        }
        
        using var checkEmailCmd = Database.GetConnection().CreateCommand();
        checkEmailCmd.CommandText = "SELECT COUNT(*) FROM `users` WHERE `email` = @email";
        checkEmailCmd.Parameters.AddWithValue("@email", email);
        var emailExists = Convert.ToInt32(checkEmailCmd.ExecuteScalar()) > 0;

        if (emailExists) {
            ViewBag.ErrorMessage = "Email už byl použit.";
            return View("/Views/Auth.cshtml");
        }
        
        // vytvoření uživatele
        using var conn = Database.GetConnection();
        if (conn == null) {
            ViewBag.ErrorMessage = "Nepodařilo se připojit k databázi.";
            return View("/Views/Auth.cshtml");
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO `users` (`username`, `email`, `password`) VALUES (@username, @email, @password)";
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@password", Utilities.EncryptPassword(password));

        if (cmd.ExecuteNonQuery() == 0) {
            ViewBag.ErrorMessage = "Nepodařilo se vytvořit uživatele.";
            return View("/Views/Auth.cshtml");
        }

        Auth.AuthUser(username, Utilities.EncryptPassword(password));
        return Redirect("/");
    }
}