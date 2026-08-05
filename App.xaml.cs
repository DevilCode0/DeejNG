using MixrU.Core.Configuration;
using MixrU.Services;
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace MixrU
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string SingleInstanceMutexName = "MixrU_SingleInstance_Mutex";
        private const string ActivateEventName = "MixrU_SingleInstance_Activate";

        private static Mutex _singleInstanceMutex;
        private static EventWaitHandle _activateEvent;
        private RegisteredWaitHandle _activateWaitHandle;
        private bool _ownsSingleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
            _ownsSingleInstanceMutex = createdNew;
            if (!createdNew)
            {
                // Another instance is already running - ask it to restore itself and exit.
                try
                {
                    using var existingEvent = EventWaitHandle.OpenExisting(ActivateEventName);
                    existingEvent.Set();
                }
                catch { }

                Shutdown();
                return;
            }

            _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
            _activateWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                _activateEvent,
                (state, timedOut) =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        (Current?.MainWindow as MainWindow)?.RestoreAndActivate();
                    });
                },
                null,
                Timeout.Infinite,
                false);

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
                    "MixrU", "settings.json");

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

            _activateWaitHandle?.Unregister(null);
            _activateEvent?.Dispose();
            if (_ownsSingleInstanceMutex)
            {
                _singleInstanceMutex?.ReleaseMutex();
            }
            _singleInstanceMutex?.Dispose();

            base.OnExit(e);
        }
    }

}
