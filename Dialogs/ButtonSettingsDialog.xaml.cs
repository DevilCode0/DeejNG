using DeejNG.Classes;
using DeejNG.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeejNG.Dialogs
{
    /// <summary>
    /// Interaction logic for ButtonSettingsDialog.
    /// Allows configuration of physical button mappings.
    /// </summary>
    public partial class ButtonSettingsDialog : Window
    {
        #region Private Fields

        private AppSettings _settings;
        private ObservableCollection<ButtonMappingViewModel> _buttonMappings = new();

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes the button settings dialog with current settings.
        /// </summary>
        public ButtonSettingsDialog(AppSettings settings)
        {
            InitializeComponent();

            _settings = settings ?? new AppSettings();

            // Load button configuration after window loads
            this.Loaded += ButtonSettingsDialog_Loaded;
        }

        private void ButtonSettingsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Load button configuration after all controls are initialized
            LoadButtonConfiguration();
        }

        #endregion

        #region Private Methods

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveButtonConfiguration();
            DialogResult = true;
            Close();
        }

        #endregion

        #region Button Configuration

        /// <summary>
        /// Loads button configuration from settings and initializes UI.
        /// Buttons are now auto-detected (10000/10001 values), so we show all 8 slots for configuration.
        /// </summary>
        private void LoadButtonConfiguration()
        {
            try
            {
                // Always show 8 button slots (max supported)
                // Users can configure ahead of time; only buttons detected from hardware will activate
                LoadButtonMappingSlots();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] LoadButtonConfiguration: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads 8 button mapping slots for configuration.
        /// Buttons are auto-detected from hardware (10000/10001 protocol).
        /// </summary>
        private void LoadButtonMappingSlots()
        {
            _buttonMappings.Clear();

            // Show all 8 button slots (users can configure ahead of time)
            const int maxButtons = 8;

            for (int i = 0; i < maxButtons; i++)
            {
                var existingMapping = _settings?.ButtonMappings?.FirstOrDefault(m => m.ButtonIndex == i);

                var viewModel = new ButtonMappingViewModel
                {
                    ButtonIndex = i,
                    Action = existingMapping?.Action ?? ButtonAction.None,
                    TargetChannelIndex = existingMapping?.TargetChannelIndex ?? -1,
                    KeyCode = existingMapping?.KeyCode ?? 0,
                    KeyModifiers = existingMapping?.KeyModifiers ?? 0,
                    KeyComboDisplay = existingMapping?.KeyComboDisplay ?? ""
                };

                _buttonMappings.Add(viewModel);
            }

            // Set ItemsSource if the control is initialized
            if (ButtonMappingsItemsControl != null)
            {
                ButtonMappingsItemsControl.ItemsSource = _buttonMappings;
            }
        }

        /// <summary>
        /// Handles button action selection changes to enable/disable channel selector.
        /// </summary>
        private void ButtonAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The binding handles this automatically via NeedsTargetChannel property
        }

        /// <summary>
        /// Validates that only numbers can be entered in numeric text boxes.
        /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Captures a key combo when the user presses a key in the shortcut capture TextBox.
        /// </summary>
        private void ButtonKeyCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb || tb.DataContext is not ButtonMappingViewModel vm) return;

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
                return;

            var m = Keyboard.Modifiers;
            int modBits = 0;
            var parts = new List<string>();
            if ((m & ModifierKeys.Control) != 0) { modBits |= 1; parts.Add("Ctrl"); }
            if ((m & ModifierKeys.Alt)     != 0) { modBits |= 2; parts.Add("Alt"); }
            if ((m & ModifierKeys.Shift)   != 0) { modBits |= 4; parts.Add("Shift"); }
            if ((m & ModifierKeys.Windows) != 0) { modBits |= 8; parts.Add("Win"); }

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            parts.Add(key.ToString());
            string display = string.Join("+", parts);

            vm.KeyCode = vk;
            vm.KeyModifiers = modBits;
            vm.KeyComboDisplay = display;
            tb.Text = display;

            e.Handled = true;
        }

        /// <summary>
        /// Saves button configuration to settings.
        /// Only saves button mappings that have actions assigned.
        /// Buttons are auto-detected from hardware (10000/10001 protocol).
        /// </summary>
        private void SaveButtonConfiguration()
        {
            _settings.ButtonMappings = new List<ButtonMapping>();

            // Only save button mappings that have actions configured
            foreach (var viewModel in _buttonMappings.Where(vm => vm.Action != ButtonAction.None))
            {
                _settings.ButtonMappings.Add(new ButtonMapping
                {
                    ButtonIndex = viewModel.ButtonIndex,
                    Action = viewModel.Action,
                    TargetChannelIndex = viewModel.TargetChannelIndex,
                    KeyCode = viewModel.KeyCode,
                    KeyModifiers = viewModel.KeyModifiers,
                    KeyComboDisplay = viewModel.KeyComboDisplay,
                    FriendlyName = $"Button {viewModel.ButtonIndex + 1}"
                });
            }
        }

        #endregion

        #region Button Mapping View Model

        /// <summary>
        /// View model for button mapping UI.
        /// </summary>
        public class ButtonMappingViewModel : INotifyPropertyChanged
        {
            // Combo index to enum mapping — skips ToggleInputOutput (=7)
            private static readonly ButtonAction[] ComboIndexToAction =
            {
                ButtonAction.None,            // 0
                ButtonAction.MediaPlayPause,  // 1
                ButtonAction.MediaNext,       // 2
                ButtonAction.MediaPrevious,   // 3
                ButtonAction.MediaStop,       // 4
                ButtonAction.MuteChannel,     // 5
                ButtonAction.GlobalMute,      // 6
                ButtonAction.KeyboardShortcut // 7
            };

            private int _buttonIndex;
            private ButtonAction _action;
            private int _targetChannelIndex;
            private uint _keyCode;
            private int _keyModifiers;
            private string _keyComboDisplay = "";

            public int ButtonIndex
            {
                get => _buttonIndex;
                set
                {
                    _buttonIndex = value;
                    OnPropertyChanged(nameof(ButtonIndex));
                    OnPropertyChanged(nameof(ButtonIndexDisplay));
                }
            }

            public string ButtonIndexDisplay => $"Button {ButtonIndex + 1}";

            public ButtonAction Action
            {
                get => _action;
                set
                {
                    _action = value;
                    OnPropertyChanged(nameof(Action));
                    OnPropertyChanged(nameof(ActionIndex));
                    OnPropertyChanged(nameof(NeedsTargetChannel));
                    OnPropertyChanged(nameof(NeedsKeyCapture));
                    OnPropertyChanged(nameof(KeyCaptureVisibility));
                }
            }

            public int ActionIndex
            {
                get
                {
                    int idx = Array.IndexOf(ComboIndexToAction, _action);
                    return idx >= 0 ? idx : 0;
                }
                set
                {
                    _action = value >= 0 && value < ComboIndexToAction.Length
                        ? ComboIndexToAction[value]
                        : ButtonAction.None;
                    OnPropertyChanged(nameof(Action));
                    OnPropertyChanged(nameof(ActionIndex));
                    OnPropertyChanged(nameof(NeedsTargetChannel));
                    OnPropertyChanged(nameof(NeedsKeyCapture));
                    OnPropertyChanged(nameof(KeyCaptureVisibility));
                }
            }

            public int TargetChannelIndex
            {
                get => _targetChannelIndex;
                set
                {
                    _targetChannelIndex = value;
                    OnPropertyChanged(nameof(TargetChannelIndex));
                    OnPropertyChanged(nameof(TargetChannelDisplay));
                }
            }

            public string TargetChannelDisplay
            {
                get => _targetChannelIndex >= 0 ? (_targetChannelIndex + 1).ToString() : "";
                set
                {
                    if (int.TryParse(value, out int channelNum) && channelNum > 0)
                        TargetChannelIndex = channelNum - 1;
                    else
                        TargetChannelIndex = -1;
                }
            }

            public uint KeyCode
            {
                get => _keyCode;
                set { _keyCode = value; OnPropertyChanged(nameof(KeyCode)); }
            }

            public int KeyModifiers
            {
                get => _keyModifiers;
                set { _keyModifiers = value; OnPropertyChanged(nameof(KeyModifiers)); }
            }

            public string KeyComboDisplay
            {
                get => _keyComboDisplay;
                set { _keyComboDisplay = value ?? ""; OnPropertyChanged(nameof(KeyComboDisplay)); }
            }

            public bool NeedsTargetChannel => _action == ButtonAction.MuteChannel;
            public bool NeedsKeyCapture    => _action == ButtonAction.KeyboardShortcut;

            public System.Windows.Visibility KeyCaptureVisibility =>
                NeedsKeyCapture ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
