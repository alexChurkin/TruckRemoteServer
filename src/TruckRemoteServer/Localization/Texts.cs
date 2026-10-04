using System;
using System.Globalization;
using System.Resources;

namespace TruckRemoteServer.Localization
{
    //Texts of the window in the chosen language (Strings*.resx; English is the neutral language).
    //Translations are embedded into the exe as Strings.<language> resources, not as satellite assemblies
    public static class Texts
    {
        private const string BASE_NAME = "TruckRemoteServer.Localization.Strings";
        private static readonly string[] Translations = { "ru", "be", "uk" };

        private static readonly ResourceManager English = new ResourceManager(BASE_NAME, typeof(Texts).Assembly);
        private static ResourceManager translation;

        static Texts()
        {
            SetLanguage("");
        }

        public static CultureInfo Culture { get; private set; }

        //"" - the language of Windows, otherwise a language code
        public static void SetLanguage(string language)
        {
            Culture = string.IsNullOrEmpty(language) ? CultureInfo.InstalledUICulture : CultureInfo.GetCultureInfo(language);
            string code = Culture.TwoLetterISOLanguageName;
            translation = Array.IndexOf(Translations, code) >= 0
                ? new ResourceManager(BASE_NAME + "." + code, typeof(Texts).Assembly)
                : null;
        }

        public static string Get(string key)
        {
            return translation?.GetString(key, CultureInfo.InvariantCulture)
                ?? English.GetString(key, CultureInfo.InvariantCulture)
                ?? key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Culture, Get(key), args);
        }
    }
}
