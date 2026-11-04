using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace GreenHellAutoStart
{
    [BepInPlugin("local.greenhell.autostart", "Green Hell Auto Start", "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> AutoContinue;
        internal static ManualLogSource PluginLog;

        private Harmony _harmony;

        private void Awake()
        {
            PluginLog = Logger;
            AutoContinue = Config.Bind("General", "AutoContinue", true,
                "Automatically continue the latest multiplayer host save after the main menu initializes.");
            ResourceControllerCoordinator.Initialize(Config);
            UIEventFindings.Initialize();
            ContextFindings.Initialize();
            GuideController.Initialize(AutoContinue);

            _harmony = new Harmony("local.greenhell.autostart");
            _harmony.PatchAll();
        }

        private void Update()
        {
            ResourceControllerCoordinator.Update();
            UIEventFindings.Update();
            ContextFindings.Update();
            GuideController.Update();
        }

        private void OnDestroy()
        {
            ResourceControllerCoordinator.Restore();
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }
}
}
