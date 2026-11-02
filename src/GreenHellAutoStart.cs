using System;
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
                "Automatically continue the latest single-player save after the main menu initializes.");

            _harmony = new Harmony("local.greenhell.autostart");
            _harmony.PatchAll();
            Logger.LogInfo("Green Hell Auto Start loaded.");
        }

        private void OnDestroy()
        {
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(MainMenu), "Update")]
    internal static class MainMenuAutoContinuePatch
    {
        private static bool _attempted;

        [HarmonyPostfix]
        private static void Postfix(MainMenu __instance)
        {
            if (Plugin.AutoContinue == null || !Plugin.AutoContinue.Value || _attempted || __instance == null)
                return;

            try
            {
                bool buttonsActive = Traverse.Create(__instance)
                    .Field("m_ButtonsActive")
                    .GetValue<bool>();
                if (!buttonsActive)
                    return;

                _attempted = true;

                GreenHellGame game = GreenHellGame.Instance;
                if (game != null && game.m_Settings != null)
                {
                    game.m_OnlyTutorial = false;
                    game.m_Settings.m_GameVisibility = P2PGameVisibility.Singleplayer;
                    Traverse.Create(typeof(MainMenuChooseMode))
                        .Field("s_DisplayMode")
                        .SetValue(MainMenuChooseMode.MainMenuChooseModeType.Singleplayer);
                }

                if (__instance.m_ContinueLogic == null)
                    return;

                __instance.m_ContinueLogic.UpdateButton();
                object saveInfo = Traverse.Create(__instance.m_ContinueLogic)
                    .Field("m_LastSavedGameInfo")
                    .GetValue();

                if (saveInfo != null)
                {
                    Plugin.PluginLog.LogInfo("Save found; continuing the latest single-player save.");
                    __instance.m_ContinueLogic.OnButtonPressed();
                }
                else
                {
                    Plugin.PluginLog.LogInfo("No single-player save found; leaving the main menu available.");
                }
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("Auto-continue failed: " + exception);
            }
        }
    }
}