using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Classes;

namespace TdA25_Error_Makers.Controllers;
using Microsoft.AspNetCore.Mvc;





public class AuthController : Controller {

    [Route("/login")]
    public IActionResult Login_Page() {
        ViewBag.AuthType = "login";
        return View("/Views/Auth.cshtml");
    } 
    
    [HttpPost("/login")]
    public IActionResult Login_Post([FromForm] string username, [FromForm] string password) {
        ViewBag.AuthType = "login";
        var acc = Auth.AuthUser(username, Utilities.EncryptPassword(password));
        if (acc == null) {
            ViewBag.ErrorMessage = "Účet s tímto jménem a heslem neexistuje.";
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

        char[] allowedChars = [
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
            'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 
            'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            '.', '_'
        ];

        if (username.Any(c => !allowedChars.Contains(c))) {
            ViewBag.ErrorMessage = "Uživatelské jméno nesmí obsahovat diakritiku, mezery a speciální znaky.";
            return View("/Views/Auth.cshtml");
        }
        
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) {
            ViewBag.ErrorMessage = "Zadejte platnou e-mailovou adresu.";
            return View("/Views/Auth.cshtml");
        }

        if (password != passwordConfirm) {
            ViewBag.ErrorMessage = "Hesla se neshodují.";
            return View("/Views/Auth.cshtml");
        }

        if (password.Length <8) {
            ViewBag.ErrorMessage = "Heslo musí obsahovat alespoň 8 znaků";
            return View("/Views/Auth.cshtml");
        }


        username = username.ToLower();
        email = email.ToLower();


        using var conn = Database.GetConnection();
        if(conn == null) {
            ViewBag.ErrorMessage = "Nepodařilo se připojit k databázi.";
            return View("/Views/Auth.cshtml");
        }

        try {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO `users` (`username`, `email`, `password`) VALUES (@username, @email, @password)";
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@password", Utilities.EncryptPassword(password));



            if (cmd.ExecuteNonQuery() == 0) {
                ViewBag.ErrorMessage = "Nepodařilo se vytvořit uživatele.";
                return View("/Views/Auth.cshtml");
            }
        } catch (MySqlException e) {
            if (e.Number == 1062) {
                ViewBag.ErrorMessage = "Uživatel s tímto jménem nebo emailem již existuje.";
                return View("/Views/Auth.cshtml");
            }


            ViewBag.ErrorMessage = "Nepodařilo se vytvořit uživatele.";
            return View("/Views/Auth.cshtml");
        }



        // všechno je ok, takže se uživatel přihlásí
        Auth.AuthUser(username, Utilities.EncryptPassword(password));
        return Redirect("/");
    }
}