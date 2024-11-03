namespace TdA25_Error_Makers.Classes;

public static class Utilities {


    public static class WebTheme {
        public static string Get() {
            // TODO: Implementovat zjištění tématu z cookies
            return "light";
        }

        public static string GetCSSPath() => $"/css/themes/{Get().ToLower()}.css";
    }




#region rozšiřující metody

    public static Value? GetValueOrNull<Key, Value>(this IDictionary<Key, Value> dictionary, in Key? key) {
        if (key == null) return default;
        return dictionary.TryGetValue(key, out var value) ? value : default;
    }

#endregion
}