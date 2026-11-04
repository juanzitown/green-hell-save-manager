using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace GHServer
{
    [BepInPlugin("local.ghserver", "GHServer", "0.1.0")]
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
            WorldUIController.Initialize(Config);
            MemoryFindings.Initialize(Config);
            GuideController.Initialize(AutoContinue);

            _harmony = new Harmony("local.ghserver");
            _harmony.PatchAll();
        }

        private void Update()
        {
            ResourceControllerCoordinator.Update();
            WorldUIController.Update();
            MemoryFindings.Update();
            GuideController.Update();
        }

        private void OnDestroy()
        {
            ResourceControllerCoordinator.Restore();
            WorldUIController.Restore();
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }
}
}
