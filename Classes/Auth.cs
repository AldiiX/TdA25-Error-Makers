using TdA25_Error_Makers.Classes.Objects;
using TdA25_Error_Makers.Services;

namespace TdA25_Error_Makers.Classes;



public static class Auth {
    public static bool UserIsLoggedIn() {
        return HCS.Current.Session.Get("loggeduser") != null;
    }

    public static Account? AuthUser(in string username, in string hashedPassword) => Account.Auth(username, hashedPassword);

    public static async Task<Account?> ReAuthUserAsync() {
        if (!UserIsLoggedIn()) return null;

        var loggedUser = HCS.Current.Session.GetObject<Account>("loggeduser");
        if (loggedUser == null) {
            //Console.WriteLine("Logged user is null");
            HCS.Current.Session.Remove("loggeduser");
            return null;
        }

        var acc = await Account.AuthAsync(loggedUser.Username, loggedUser.Password);
        if (acc == null) {
            //Console.WriteLine("Acc is null");
            HCS.Current.Session.Remove("loggeduser");
            return null;
        }

        HCS.Current.Session.SetObject("loggeduser", acc);
        return acc;
    }

    public static Account? ReAuthUser() => ReAuthUserAsync().Result;
}