using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Classes.Objects;
using TdA25_Error_Makers.Services;

namespace TdA25_Error_Makers.Classes;





public static class Utilities {


    public static class Cookie {
        public static void Set(in string key, in string? value, in bool saveToTemp = true) {
            if (saveToTemp && HCS.Current.Items["tempcookie_" + key] != null) return;

            HCS.Current.Items["tempcookie_" + key] = value;
            HCS.Current.Response.Cookies.Append(key, value ?? "null", new CookieOptions() {
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(365),
            });
        }

        public static string? Get(in string key) => HCS.Current.Request.Cookies[key];

        public static bool Exists(in string key) => HCS.Current.Request.Cookies.ContainsKey(key);

        public static void Delete(in string key) {
            HCS.Current.Response.Cookies.Append(key, "", new CookieOptions() {
                IsEssential = true,
                Expires = DateTime.UtcNow.AddDays(-1),
            });
        }

        public static void Remove(in string key) => Delete(key);
    }

    public static class WebTheme {
        public static void Set(in string theme) => Cookie.Set("webtheme", theme);

        public static string Get() {
            var wt = Cookie.Get("webtheme");
            if(wt == null) Cookie.Set("webtheme", "light");

            return wt switch {
                "light" => "light",
                "dark" => "dark",
                _ => "light"
            };
        }

        public static string GetCSSFile() => Get() == "light" ? "/css/themes/light.css" : "/css/themes/dark.css";
    }





#region rozšiřující metody

    public static Value? GetValueOrNull<Key, Value>(this IDictionary<Key, Value> dictionary, in Key? key) {
        if (key == null) return default;
        return dictionary.TryGetValue(key, out var value) ? value : default;
    }

    public static T? GetValueOrNull<T>(this MySqlDataReader reader, string key) {
        if(reader.IsDBNull(key)) return default;
        return (T)reader[key];
    }

    public static Account GetLoggedAccountFromContext() {
        if(HCS.Current.Items["loggeduser"] is not Account account) throw new Exception("Account not found in context");
        return account;
    }

    public static Account? GetLoggedAccountFromContextOrNull() {
        return HCS.Current.Items["loggeduser"] is not Account account ? null : account;
    }

    private static string EncryptWithSHA512(in string password) {
        using SHA512 sha512 = SHA512.Create();
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] sha512HashBytes = sha512.ComputeHash(passwordBytes);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in sha512HashBytes) {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private static string EncryptWithMD5(in string password) {
        using MD5 md5 = MD5.Create();
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] md5HashBytes = md5.ComputeHash(passwordBytes);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in md5HashBytes) {
            sb.Append(b.ToString("x2"));
        }


        return sb.ToString();
    }

    public static string EncryptPassword(in string password) => EncryptWithSHA512(password) + EncryptWithMD5(password[0] + "" + password[2]);

    public static void SetObject<T>(this ISession session, in string key, in T value) {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObject<T>(this ISession session, in string key) where T : class? {
        var value = session.GetString(key);
        return value == null ? null : JsonSerializer.Deserialize<T>(value);
    }

    public static T? GetObject<T>(this ISession session, string key) where T : struct {
        var value = session.GetString(key);
        return value == null ? (T?)null : JsonSerializer.Deserialize<T>(value);
    }
#endregion
}