using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GHServer
{
    internal static class WorldUIController
    {
        private static ConfigEntry<bool> _disableWorldUI;
        private static ConfigEntry<bool> _aggressiveVisualCleanup;
        private static ConfigEntry<bool> _destroyVisualComponents;
        private static readonly Dictionary<Canvas, bool> OriginalStates = new Dictionary<Canvas, bool>();
        private static readonly Dictionary<Renderer, bool> OriginalRendererStates = new Dictionary<Renderer, bool>();
        private static readonly Dictionary<Light, bool> OriginalLightStates = new Dictionary<Light, bool>();
        private static float _nextCleanupTime;
        private static bool _unloadRequested;

        internal static void Initialize(ConfigFile config)
        {
            _disableWorldUI = config.Bind("Graphics", "DisableWorldUI", true,
                "Disable world HUD and overlay canvases after the multiplayer world starts.");
            _aggressiveVisualCleanup = config.Bind("Graphics", "AggressiveVisualCleanup", false,
                "Disable world renderers and lights after startup while keeping gameplay entities and calculations alive.");
            _destroyVisualComponents = config.Bind("Graphics", "DestroyVisualComponents", false,
                "Aggressively destroy world renderers, lights, and canvases, then unload unused assets. Restart the game to restore them.");
        }

        internal static void Update()
        {
            if (_disableWorldUI == null || !_disableWorldUI.Value)
                return;

            if (Time.unscaledTime < _nextCleanupTime)
                return;
            _nextCleanupTime = Time.unscaledTime + 5f;

            MainMenuManager menuManager = MainMenuManager.Get();
            bool worldStarted = menuManager != null
                && HarmonyLib.Traverse.Create(menuManager).Method("IsGameStarted").GetValue<bool>();
            if (!worldStarted)
                worldStarted = SceneManager.GetActiveScene().name == "Level";
            if (!worldStarted)
                return;

            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.gameObject.scene.IsValid())
                    continue;
                if (!OriginalStates.ContainsKey(canvas))
                    OriginalStates.Add(canvas, canvas.enabled);
                canvas.enabled = false;
            }

            if (_aggressiveVisualCleanup.Value)
            {
                if (_destroyVisualComponents.Value)
                    DestroyVisualComponents();
                else
                    DisableVisualComponents();
            }
        }

        private static void DestroyVisualComponents()
        {
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (Canvas canvas in canvases)
                if (canvas != null && canvas.gameObject.scene.IsValid())
                    UnityEngine.Object.Destroy(canvas);

            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            foreach (Renderer renderer in renderers)
                if (renderer != null && renderer.gameObject.scene.IsValid())
                    UnityEngine.Object.Destroy(renderer);

            Light[] lights = Resources.FindObjectsOfTypeAll<Light>();
            foreach (Light light in lights)
                if (light != null && light.gameObject.scene.IsValid())
                    UnityEngine.Object.Destroy(light);

            if (!_unloadRequested)
            {
                _unloadRequested = true;
                Resources.UnloadUnusedAssets();
                GC.Collect();
                Plugin.PluginLog.LogWarning("Destroyed visual components and requested unused-asset unload.");
            }
        }

        private static void DisableVisualComponents()
        {
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.gameObject.scene.IsValid())
                    continue;
                if (!OriginalRendererStates.ContainsKey(renderer))
                    OriginalRendererStates.Add(renderer, renderer.enabled);
                renderer.enabled = false;
            }

            Light[] lights = Resources.FindObjectsOfTypeAll<Light>();
            foreach (Light light in lights)
            {
                if (light == null || !light.gameObject.scene.IsValid())
                    continue;
                if (!OriginalLightStates.ContainsKey(light))
                    OriginalLightStates.Add(light, light.enabled);
                light.enabled = false;
            }
        }

        internal static void Restore()
        {
            foreach (KeyValuePair<Canvas, bool> state in OriginalStates)
            {
                if (state.Key != null)
                    state.Key.enabled = state.Value;
            }
            OriginalStates.Clear();
            foreach (KeyValuePair<Renderer, bool> state in OriginalRendererStates)
            {
                if (state.Key != null)
                    state.Key.enabled = state.Value;
            }
            OriginalRendererStates.Clear();
            foreach (KeyValuePair<Light, bool> state in OriginalLightStates)
            {
                if (state.Key != null)
                    state.Key.enabled = state.Value;
            }
            OriginalLightStates.Clear();
            _unloadRequested = false;
        }
    }
}
