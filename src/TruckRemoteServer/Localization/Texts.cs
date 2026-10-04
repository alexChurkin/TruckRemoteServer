using System.Globalization;
using System.Resources;

namespace TruckRemoteServer.Localization
{
    //Texts of the window in the chosen language (Strings*.resx; English is the neutral language)
    public static class Texts
    {
        private static readonly ResourceManager Resources =
            new ResourceManager("TruckRemoteServer.Localization.Strings", typeof(Texts).Assembly);

        public static CultureInfo Culture { get; private set; } = CultureInfo.CurrentUICulture;

        //"" - the language of Windows, otherwise a language code
        public static void SetLanguage(string language)
        {
            Culture = string.IsNullOrEmpty(language) ? CultureInfo.InstalledUICulture : CultureInfo.GetCultureInfo(language);
        }

        public static string Get(string key)
        {
            return Resources.GetString(key, Culture) ?? key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Culture, Get(key), args);
        }
    }
}
