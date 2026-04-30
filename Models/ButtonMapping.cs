namespace DeejNG.Models
{
    /// <summary>
    /// Represents the mapping configuration for a physical button.
    /// </summary>
    public class ButtonMapping
    {
        /// <summary>
        /// Gets or sets the button index (0-based).
        /// </summary>
        public int ButtonIndex { get; set; }

        /// <summary>
        /// Gets or sets the action to perform when the button is pressed.
        /// </summary>
        public ButtonAction Action { get; set; } = ButtonAction.None;

        /// <summary>
        /// Gets or sets the target channel index for channel-specific actions (0-based).
        /// -1 indicates no specific channel (for global actions).
        /// </summary>
        public int TargetChannelIndex { get; set; } = -1;

        /// <summary>
        /// Gets or sets a friendly name for this button mapping.
        /// </summary>
        public string FriendlyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Win32 virtual-key code for KeyboardShortcut action.
        /// </summary>
        public uint KeyCode { get; set; } = 0;

        /// <summary>
        /// Gets or sets the modifier bitmask for KeyboardShortcut action (1=Ctrl, 2=Alt, 4=Shift, 8=Win).
        /// </summary>
        public int KeyModifiers { get; set; } = 0;

        /// <summary>
        /// Gets or sets the human-readable display string for the key combo (e.g. "Ctrl+C").
        /// </summary>
        public string KeyComboDisplay { get; set; } = string.Empty;
    }
}
