using System;
using System.Linq;
using System.Windows;

namespace DeejNG.Services
{
    public class LocalizationManager
    {
        private static LocalizationManager _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        public string CurrentLanguage { get; private set; } = "en";

        public event Action LanguageChanged;

        public void SetLanguage(string language)
        {
            if (language != "en" && language != "ar")
                language = "en";

            CurrentLanguage = language;

            var dicts = Application.Current.Resources.MergedDictionaries;

            var existing = dicts.FirstOrDefault(d =>
                d.Source?.OriginalString?.Contains("/Localization/Strings.") == true);
            if (existing != null)
                dicts.Remove(existing);

            dicts.Add(new ResourceDictionary
            {
                Source = new Uri($"/Localization/Strings.{language}.xaml", UriKind.Relative)
            });

            LanguageChanged?.Invoke();
        }

        public string GetString(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }

        public string GetString(string key, params object[] args)
        {
            var template = GetString(key);
            try { return string.Format(template, args); }
            catch { return template; }
        }

        public static string L(string key) => Instance.GetString(key);
        public static string L(string key, params object[] args) => Instance.GetString(key, args);
    }
}
