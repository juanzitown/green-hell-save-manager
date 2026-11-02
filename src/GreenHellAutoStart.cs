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
        internal static ConfigEntry<bool> LogMenuEvents;
        internal static ManualLogSource PluginLog;

        private Harmony _harmony;

        private void Awake()
        {
            PluginLog = Logger;
            AutoContinue = Config.Bind("General", "AutoContinue", true,
                "Automatically continue the latest multiplayer host save after the main menu initializes.");
            LogMenuEvents = Config.Bind("General", "LogMenuEvents", false,
                "Log menu screen transitions and button clicks while navigating automatically.");
            LowerResources.Initialize(Config);

            _harmony = new Harmony("local.greenhell.autostart");
            _harmony.PatchAll();
            Logger.LogInfo("Green Hell Auto Start loaded.");
        }

        private void Update()
        {
            LowerResources.Update();
        }

        private void OnDestroy()
        {
            LowerResources.Restore();
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), "Update")]
    internal static class MultiplayerAutoContinuePatch
    {
        private static int _stage;
        private static int _lastLoggedStage = -1;
        private static Type _lastLoggedScreen;
        private static bool _lastLoggedTransitionState;
        private static bool _lastLoggedInputBlocked;

        [HarmonyPostfix]
        private static void Postfix(MainMenuManager __instance)
        {
            if (__instance == null)
                return;

            if (Plugin.LogMenuEvents != null && Plugin.LogMenuEvents.Value)
                UIUtils.CaptureMenuEvents(__instance);

            if (Plugin.AutoContinue == null || !Plugin.AutoContinue.Value || _stage >= 3)
                return;

            try
            {
                bool changingScreens = Traverse.Create(__instance)
                    .Method("IsChangingScreens")
                    .GetValue<bool>();
                bool uiInputsBlocked = Traverse.Create(__instance)
                    .Method("IsUIInputsBlocked")
                    .GetValue<bool>();
                Type activeScreen = Traverse.Create(__instance)
                    .Method("GetActiveScreen")
                    .GetValue<Type>();
                if (_lastLoggedStage != _stage || _lastLoggedScreen != activeScreen
                    || _lastLoggedTransitionState != changingScreens
                    || _lastLoggedInputBlocked != uiInputsBlocked)
                {
                    Plugin.PluginLog.LogInfo("Auto sequence state: stage=" + _stage
                        + ", screen=" + (activeScreen == null ? "<none>" : activeScreen.Name)
                        + ", changingScreens=" + changingScreens
                        + ", uiInputsBlocked=" + uiInputsBlocked);
                    _lastLoggedStage = _stage;
                    _lastLoggedScreen = activeScreen;
                    _lastLoggedTransitionState = changingScreens;
                    _lastLoggedInputBlocked = uiInputsBlocked;
                }

                if (uiInputsBlocked)
                    return;

                if (_stage == 0 && activeScreen == typeof(MainMenu))
                {
                    MainMenu mainMenu = (MainMenu)Traverse.Create(__instance)
                        .Method("GetScreen", typeof(MainMenu))
                        .GetValue();
                    if (mainMenu == null || !Traverse.Create(mainMenu).Field("m_ButtonsActive").GetValue<bool>())
                        return;

                    Plugin.PluginLog.LogInfo("Selecting Multiplayer.");
                    Traverse.Create(mainMenu).Method("OnMultiplayer").GetValue();
                    _stage = 1;
                    return;
                }

                if (_stage == 1 && activeScreen == typeof(MainMenuJoinOrHost))
                {
                    MainMenuJoinOrHost joinOrHost = (MainMenuJoinOrHost)Traverse.Create(__instance)
                        .Method("GetScreen", typeof(MainMenuJoinOrHost))
                        .GetValue();
                    if (joinOrHost == null)
                        return;

                    Plugin.PluginLog.LogInfo("Selecting Host Game.");
                    Traverse.Create(joinOrHost).Method("OnHost").GetValue();
                    _stage = 2;
                    return;
                }

                if (_stage != 2 || activeScreen != typeof(MainMenuChooseMode))
                    return;

                MainMenuChooseMode chooseMode = (MainMenuChooseMode)Traverse.Create(__instance)
                    .Method("GetScreen", typeof(MainMenuChooseMode))
                    .GetValue();
                if (chooseMode == null)
                    return;

                ContinueButtonLogic continueLogic = Traverse.Create(chooseMode)
                    .Field("m_ContinueLogic")
                    .GetValue<ContinueButtonLogic>();
                if (continueLogic == null)
                    return;

                continueLogic.UpdateButton();
                object saveInfo = Traverse.Create(continueLogic)
                    .Field("m_LastSavedGameInfo")
                    .GetValue();

                _stage = 3;
                if (saveInfo != null)
                {
                    Plugin.PluginLog.LogInfo("Multiplayer save found; continuing the hosted world.");
                    Traverse.Create(chooseMode).Method("OnContinue").GetValue();
                }
                else
                {
                    Plugin.PluginLog.LogInfo("No multiplayer save found; leaving the host menu available.");
                }
            }
            catch (Exception exception)
            {
                _stage = 3;
                Plugin.PluginLog.LogError("Auto-continue failed: " + exception);
            }
        }

    }
}