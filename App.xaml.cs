using DeejNG.Core.Configuration;
using DeejNG.Services;
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace DeejNG
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ServiceLocator.Configure();

            // Apply language before MainWindow constructs so DynamicResource bindings
            // resolve in the correct language on first render (no visible re-render).
            LocalizationManager.Instance.SetLanguage(DetectStartupLanguage());
        }
        
        private static string DetectStartupLanguage()
        {
            try
            {
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DeejNG", "settings.json");

                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Language", out var langEl))
                    {
                        string saved = langEl.GetString();
                        if (saved == "en" || saved == "ar")
                            return saved;
                    }
                }
            }
            catch { }

            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? "ar" : "en";
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cleanup services
            ServiceLocator.Dispose();
            base.OnExit(e);
        }
    }

}
