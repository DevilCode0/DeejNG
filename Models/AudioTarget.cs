using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeejNG.Models
{
    /// <summary>
    /// Represents a target for audio control (e.g., an app session, input device, or output device).
    /// Used to define what a channel or slider will control.
    /// </summary>
    public class AudioTarget
    {
        #region Public Properties

        /// <summary>
        /// Indicates whether the target is an input device (e.g., a microphone).
        /// </summary>
        public bool IsInputDevice { get; set; } = false;

        /// <summary>
        /// Indicates whether the target is an output device (e.g., speakers or headphones).
        /// </summary>
        public bool IsOutputDevice { get; set; } = false;

        /// <summary>
        /// Indicates whether this target is a VoiceMeeter bus (Hardware Out A1, A2, … or B1, B2, …).
        /// When true, <see cref="BusIndex"/> identifies which Bus[n] to control via the VoiceMeeter Remote API.
        /// </summary>
        public bool IsVoiceMeeterBus { get; set; } = false;

        /// <summary>
        /// Zero-based VoiceMeeter bus index (only used when <see cref="IsVoiceMeeterBus"/> is true).
        /// Corresponds to Bus[n].Gain / Bus[n].Mute in the VoiceMeeter Remote API.
        /// </summary>
        public int BusIndex { get; set; } = 0;

        /// <summary>
        /// The name of the audio target (e.g., "Spotify", "Microphone", "Speakers").
        /// </summary>
        public string Name { get; set; } = "";

        #endregion Public Properties
    }

}