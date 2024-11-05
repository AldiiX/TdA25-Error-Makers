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

#endregion
}