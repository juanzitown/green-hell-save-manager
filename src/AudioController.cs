using BepInEx.Configuration;
using UnityEngine;

namespace GreenHellAutoStart
{
    internal static class AudioController
    {
        private static ConfigEntry<bool> _muteAudio;
        private static bool _originalAudioPaused;
        private static bool _originalStateCaptured;
        private static bool _stateLogged;

        internal static void Initialize(ConfigFile config)
        {
            _muteAudio = config.Bind("Audio", "MuteAudio", true,
                "Pause Unity audio playback while the plugin is active.");
            _originalAudioPaused = AudioListener.pause;
            _originalStateCaptured = true;
        }

        internal static void Update()
        {
            AudioListener.pause = _muteAudio.Value || _originalAudioPaused;
            if (_stateLogged)
                return;

            Plugin.PluginLog.LogInfo("Audio paused: " + AudioListener.pause + ".");
            _stateLogged = true;
        }

        internal static void Restore()
        {
            if (!_originalStateCaptured)
                return;

            AudioListener.pause = _originalAudioPaused;
            _originalStateCaptured = false;
        }
    }
}