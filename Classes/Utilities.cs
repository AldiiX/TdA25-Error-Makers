namespace TdA25_Error_Makers.Classes;

public class Utilities {


    public static class WebTheme {
        public static string Get() {
            // TODO: Implementovat zjištění tématu z cookies
            return "light";
        }

        public static string GetCSSPath() => $"/css/themes/{Get().ToLower()}.css";
    }
}