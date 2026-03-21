using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace DeejNG.Services
{
    /// <summary>
    /// Wraps the VoiceMeeter Remote API (VoicemeeterRemote64.dll) to allow controlling
    /// VoiceMeeter bus (hardware out) volumes from DeejNG sliders.
    /// Loads the DLL at runtime from the VoiceMeeter install directory.
    /// </summary>
    public class VoiceMeeterService : IDisposable
    {
        #region Private Fields

        private IntPtr _dllHandle = IntPtr.Zero;
        private bool _loggedIn = false;
        private int _vmType = 0; // 1=VoiceMeeter, 2=Banana, 3=Potato

        // Delegate types matching the VoiceMeeter Remote API C signatures
        private delegate int VBVMR_Login();
        private delegate int VBVMR_Logout();
        private delegate int VBVMR_GetVoicemeeterType(ref int nType);
        private delegate int VBVMR_SetParameterFloat(
            [MarshalAs(UnmanagedType.LPStr)] string szParamName, float value);
        private delegate int VBVMR_GetParameterFloat(
            [MarshalAs(UnmanagedType.LPStr)] string szParamName, ref float pValue);

        private VBVMR_Login _login;
        private VBVMR_Logout _logout;
        private VBVMR_GetVoicemeeterType _getType;
        private VBVMR_SetParameterFloat _setFloat;
        private VBVMR_GetParameterFloat _getFloat;

        #endregion Private Fields

        #region Public Properties

        /// <summary>True if VoiceMeeter was found, the DLL loaded, and login succeeded.</summary>
        public bool IsAvailable => _loggedIn;

        /// <summary>1 = VoiceMeeter, 2 = Banana, 3 = Potato. 0 if unknown.</summary>
        public int VoiceMeeterType => _vmType;

        #endregion Public Properties

        #region Public Constructors

        public VoiceMeeterService()
        {
            TryLoad();
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Sets the gain for a VoiceMeeter bus using a normalized level (0.0–1.0).
        /// Maps 0.0 → −60 dB (silence) and 1.0 → 0 dB (unity gain).
        /// </summary>
        public bool SetBusGain(int busIndex, float level)
        {
            if (!_loggedIn || _setFloat == null) return false;
            float gainDb = level <= 0f ? -60f : Math.Clamp(level * 60f - 60f, -60f, 12f);
            int result = _setFloat($"Bus[{busIndex}].Gain", gainDb);
#if DEBUG
            Debug.WriteLine($"[VoiceMeeter] SetBusGain Bus[{busIndex}] level={level:F2} gain={gainDb:F1}dB result={result}");
#endif
            return result == 0;
        }

        /// <summary>Sets the mute state for a VoiceMeeter bus.</summary>
        public bool SetBusMute(int busIndex, bool mute)
        {
            if (!_loggedIn || _setFloat == null) return false;
            return _setFloat($"Bus[{busIndex}].Mute", mute ? 1f : 0f) == 0;
        }

        /// <summary>Gets the current gain of a bus as a normalized 0.0–1.0 level.</summary>
        public bool TryGetBusGain(int busIndex, out float level)
        {
            level = 0f;
            if (!_loggedIn || _getFloat == null) return false;
            float gainDb = 0f;
            if (_getFloat($"Bus[{busIndex}].Gain", ref gainDb) != 0) return false;
            level = Math.Clamp((gainDb + 60f) / 60f, 0f, 1f);
            return true;
        }

        /// <summary>
        /// Returns the bus labels for the detected VoiceMeeter type.
        /// Indices match the Bus[n] parameter used in the API.
        /// </summary>
        public string[] GetBusLabels()
        {
            return _vmType switch
            {
                2 => new[] { "A1", "A2", "A3", "B1", "B2" },           // Banana: 5 buses
                3 => new[] { "A1", "A2", "A3", "A4", "A5", "B1", "B2", "B3" }, // Potato: 8 buses
                _ => new[] { "A1", "B1" }                               // VoiceMeeter: 2 buses
            };
        }

        public void Dispose()
        {
            if (_loggedIn && _logout != null)
            {
                try { _logout(); } catch { }
                _loggedIn = false;
            }

            if (_dllHandle != IntPtr.Zero)
            {
                try { NativeLibrary.Free(_dllHandle); } catch { }
                _dllHandle = IntPtr.Zero;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void TryLoad()
        {
            try
            {
                string dllPath = FindDllPath();
                if (dllPath == null)
                {
#if DEBUG
                    Debug.WriteLine("[VoiceMeeter] DLL not found – VoiceMeeter not installed or not detected");
#endif
                    return;
                }

                _dllHandle = NativeLibrary.Load(dllPath);
                if (_dllHandle == IntPtr.Zero) return;

                _login   = GetExport<VBVMR_Login>("VBVMR_Login");
                _logout  = GetExport<VBVMR_Logout>("VBVMR_Logout");
                _getType = GetExport<VBVMR_GetVoicemeeterType>("VBVMR_GetVoicemeeterType");
                _setFloat = GetExport<VBVMR_SetParameterFloat>("VBVMR_SetParameterFloat");
                _getFloat = GetExport<VBVMR_GetParameterFloat>("VBVMR_GetParameterFloat");

                if (_login == null || _setFloat == null) return;

                int loginResult = _login();
                // 0 = OK, 1 = OK but VoiceMeeter needs restart — both mean we can use the API
                if (loginResult >= 0)
                {
                    _loggedIn = true;
                    int type = 0;
                    _getType?.Invoke(ref type);
                    _vmType = type;
#if DEBUG
                    Debug.WriteLine($"[VoiceMeeter] Loaded OK – type={_vmType} ({GetTypeName()})");
#endif
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[VoiceMeeter] Load failed: {ex.Message}");
#endif
            }
        }

        private T GetExport<T>(string name) where T : Delegate
        {
            if (!NativeLibrary.TryGetExport(_dllHandle, name, out var ptr)) return null;
            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        private static string FindDllPath()
        {
            // Try known registry locations for the VoiceMeeter install directory
            string[] regPaths =
            {
                @"SOFTWARE\VB-Audio\Voicemeeter",
                @"SOFTWARE\WOW6432Node\VB-Audio\Voicemeeter",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VB:Voicemeeter {17359A74-1236-5467}",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\VB:Voicemeeter {17359A74-1236-5467}",
            };

            foreach (var regPath in regPaths)
            {
                using var key = Registry.LocalMachine.OpenSubKey(regPath);
                if (key == null) continue;

                string dir = key.GetValue("INSTALLDIR") as string
                          ?? key.GetValue("UninstallString") as string;

                if (string.IsNullOrEmpty(dir)) continue;

                // UninstallString points to an .exe — get its directory
                if (dir.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    dir = Path.GetDirectoryName(dir);

                string dll = Path.Combine(dir ?? "", "VoicemeeterRemote64.dll");
                if (File.Exists(dll)) return dll;

                dll = Path.Combine(dir ?? "", "VoicemeeterRemote.dll");
                if (File.Exists(dll)) return dll;
            }

            // Fall back to common default install paths
            string[] defaults =
            {
                @"C:\Program Files (x86)\VB\Voicemeeter\VoicemeeterRemote64.dll",
                @"C:\Program Files\VB\Voicemeeter\VoicemeeterRemote64.dll",
            };

            foreach (var p in defaults)
                if (File.Exists(p)) return p;

            return null;
        }

        private string GetTypeName() => _vmType switch
        {
            1 => "VoiceMeeter",
            2 => "VoiceMeeter Banana",
            3 => "VoiceMeeter Potato",
            _ => "Unknown"
        };

        #endregion Private Methods
    }
}
