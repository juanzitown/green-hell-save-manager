using BepInEx.Configuration;
using UnityEngine;

namespace GreenHellAutoStart
{
    internal static class PerformanceController
    {
        private static ConfigEntry<int> _targetFrameRate;
        private static ConfigEntry<bool> _runInBackground;
        private static ConfigEntry<int> _screenWidth;
        private static ConfigEntry<int> _screenHeight;
        private static bool _originalStateCaptured;
        private static bool _settingsLogged;
        private static bool _resolutionApplied;
        private static int _originalTargetFrameRate;
        private static int _originalVSyncCount;
        private static bool _originalRunInBackground;
        private static int _originalScreenWidth;
        private static int _originalScreenHeight;
        private static bool _originalFullscreen;

        internal static void Initialize(ConfigFile config)
        {
            _targetFrameRate = config.Bind("Performance", "TargetFrameRate", 15,
                "Maximum Unity frame rate; set to 0 to leave the current frame rate unchanged.");
            _runInBackground = config.Bind("Performance", "RunInBackground", true,
                "Keep game simulation running when the game window is not focused.");
            _screenWidth = config.Bind("Display", "ScreenWidth", 480,
                "Window width to request at game startup.");
            _screenHeight = config.Bind("Display", "ScreenHeight", 320,
                "Window height to request at game startup.");

            _originalTargetFrameRate = Application.targetFrameRate;
            _originalVSyncCount = QualitySettings.vSyncCount;
            _originalRunInBackground = Application.runInBackground;
            _originalScreenWidth = Screen.width;
            _originalScreenHeight = Screen.height;
            _originalFullscreen = Screen.fullScreen;
            _originalStateCaptured = true;
        }

        internal static void Update()
        {
            int targetFrameRate = Mathf.Clamp(_targetFrameRate.Value, 0, 240);
            if (targetFrameRate > 0)
            {
                Application.targetFrameRate = targetFrameRate;
                QualitySettings.vSyncCount = 0;
            }
            else
            {
                Application.targetFrameRate = _originalTargetFrameRate;
                QualitySettings.vSyncCount = _originalVSyncCount;
            }

            Application.runInBackground = _runInBackground.Value;
            if (!_resolutionApplied)
            {
                int width = Mathf.Clamp(_screenWidth.Value, 320, 7680);
                int height = Mathf.Clamp(_screenHeight.Value, 240, 4320);
                Screen.SetResolution(width, height, false);
                Plugin.PluginLog.LogInfo("Requested window resolution: " + width + "x" + height + ".");
                _resolutionApplied = true;
            }

            if (_settingsLogged)
                return;

            Plugin.PluginLog.LogInfo("Resource settings: targetFrameRate=" + targetFrameRate
                + ", vSyncCount=" + QualitySettings.vSyncCount
                + ", runInBackground=" + Application.runInBackground
                + ", resolution=" + _screenWidth.Value + "x" + _screenHeight.Value + ".");
            _settingsLogged = true;
        }

        internal static void Restore()
        {
            if (!_originalStateCaptured)
                return;

            Application.targetFrameRate = _originalTargetFrameRate;
            QualitySettings.vSyncCount = _originalVSyncCount;
            Application.runInBackground = _originalRunInBackground;
            Screen.SetResolution(_originalScreenWidth, _originalScreenHeight, _originalFullscreen);
            _originalStateCaptured = false;
        }
    }
}