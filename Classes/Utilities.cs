using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;
using TdA25_Error_Makers.Classes.Objects;

namespace TdA25_Error_Makers.Classes;





public static class Utilities {

    public const string DEFAULT_WEBTHEME = "light";


    public static class Cookie {
        public static void Set(in string key, in string? value, in bool saveToTemp = true, in CookieOptions? cookieOptions = null) {
            if (saveToTemp && HCS.Current.Items["tempcookie_" + key] != null) return;

            HCS.Current.Items["tempcookie_" + key] = value;
            HCS.Current.Response.Cookies.Append(key, value ?? "null", cookieOptions ?? new CookieOptions() {
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
        public static void Set(in string theme) {
            string fullDomain = HCS.Current.Request.Host.Host;

            string[] domainParts = fullDomain.Split('.');
            string rootDomain;

            if (domainParts.Length >= 2) rootDomain = domainParts[^2] + "." + domainParts[^1];
            else rootDomain = fullDomain;


            Cookie.Set("webtheme", theme, true, new CookieOptions() {
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(365),
                Domain = rootDomain,
            });
        }

        public static string Get() {
            var wt = Cookie.Get("webtheme");
            if (wt == null) WebTheme.Set(DEFAULT_WEBTHEME);

            return wt switch {
                "light" => "light",
                "dark" => "dark",
                _ => DEFAULT_WEBTHEME
            };
        }

        public static string GetCSSFile() {
            return Get() switch {
                "light" => "/css/themes/light.css",
                "dark" => "/css/themes/dark.css",
                _ => $"/css/themes/{DEFAULT_WEBTHEME.ToLower()}.css"
            };
        }
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

    public static string DeepClone(this string str) {
        return new string(str);
    }
#endregion

#region normální metody

    public static string SetActiveClass(string p) {
        string path = HCS.Current.Request.Path.ToString();

        if (path == p) return "active";
        return "";
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

    public static string EncryptPassword(in string password) => EncryptWithSHA512(password) + EncryptWithMD5(password[0] + "" + password[1] + "" + password[^1]);


#endregion
}