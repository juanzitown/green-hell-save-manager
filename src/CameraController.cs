using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GreenHellAutoStart
{
    internal static class CameraController
    {
        private static ConfigEntry<bool> _disableCameraRendering;
        private static ConfigEntry<bool> _disableMenuCamera;
        private static ConfigEntry<bool> _logCameraInventory;
        private static readonly Dictionary<Camera, bool> OriginalCameraStates = new Dictionary<Camera, bool>();
        private static Camera _gameplayCamera;
        private static bool _cameraShutdownReadyLogged;
        private static bool _eventsSubscribed;
        private static bool _hasLoggedWorldReady;
        private static bool _lastWorldReady;
        private static float _nextCameraScanTime;
        private static float _nextStatusLogTime;
        private static float _nextCameraInventoryLogTime;
        private static string _cameraLogPath;
        private static string _lastCameraReadiness;

        internal static void Initialize(ConfigFile config)
        {
            _disableCameraRendering = config.Bind("Graphics", "DisableCameraRendering", true,
                "Disable the gameplay Main Camera once it appears in the world. Loading and Notepad cameras stay enabled.");
            _disableMenuCamera = config.Bind("Graphics", "DisableMenuCamera", true,
                "Disable the MainMenu scene camera after its screen is ready. Menu transitions remain programmatic.");
            _logCameraInventory = config.Bind("Graphics", "LogCameraInventory", false,
                "Write all scene cameras to a separate log and pause camera disabling while enabled.");

            _cameraLogPath = Path.Combine(Paths.BepInExRootPath, "GreenHellAutoStart-Camera.log");
            try
            {
                File.WriteAllText(_cameraLogPath, "Camera inventory log started: "
                    + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
                WriteCameraLog("Camera disabling is paused while LogCameraInventory is enabled.");
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("Could not initialize camera inventory log: " + exception);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            _eventsSubscribed = true;
        }

        internal static void Update()
        {
            MainMenuManager menuManager = MainMenuManager.Get();
            string readinessSource;
            bool worldReady = IsWorldReady(menuManager, out readinessSource);
            if (!_hasLoggedWorldReady || worldReady != _lastWorldReady)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                Plugin.PluginLog.LogInfo("World transition: worldReady=" + worldReady
                    + ", source=" + readinessSource
                    + ", managerPresent=" + (menuManager != null)
                    + ", activeScene=" + activeScene.name);
                _lastWorldReady = worldReady;
                _hasLoggedWorldReady = true;
            }

            if (Time.unscaledTime >= _nextStatusLogTime)
            {
                _nextStatusLogTime = Time.unscaledTime + 10f;
                LogWorldStatus(menuManager, worldReady, readinessSource);
            }

            if (_logCameraInventory.Value)
            {
                if (Time.unscaledTime >= _nextCameraInventoryLogTime)
                {
                    _nextCameraInventoryLogTime = Time.unscaledTime + 1f;
                    LogCameraInventory("periodic");
                }
                return;
            }

            if (_disableCameraRendering.Value && _disableMenuCamera.Value)
                DisableReadyMenuCamera();

            if (!worldReady)
                return;

            if (!_disableCameraRendering.Value)
            {
                RestoreCameras();
                return;
            }

            if (Time.unscaledTime < _nextCameraScanTime)
                return;
            _nextCameraScanTime = Time.unscaledTime + 1f;

            string cameraReadiness;
            if (!IsCameraShutdownReady(out _gameplayCamera, out cameraReadiness))
            {
                if (!string.Equals(_lastCameraReadiness, cameraReadiness, StringComparison.Ordinal))
                {
                    Plugin.PluginLog.LogInfo("Waiting to disable gameplay camera: " + cameraReadiness);
                    _lastCameraReadiness = cameraReadiness;
                }
                return;
            }

            if (!_cameraShutdownReadyLogged)
            {
                Plugin.PluginLog.LogInfo("Camera shutdown ready: " + cameraReadiness
                    + ", mainCamera=" + _gameplayCamera.name
                    + ", scene=" + _gameplayCamera.gameObject.scene.name);
                _cameraShutdownReadyLogged = true;
            }

            if (!OriginalCameraStates.ContainsKey(_gameplayCamera))
            {
                OriginalCameraStates.Add(_gameplayCamera, _gameplayCamera.enabled);
                Plugin.PluginLog.LogInfo("Disabling gameplay camera rendering: " + _gameplayCamera.name);
            }
            else if (_gameplayCamera.enabled)
            {
                Plugin.PluginLog.LogInfo("Gameplay Main Camera became active again; disabling it: " + _gameplayCamera.name);
            }

            _gameplayCamera.enabled = false;
        }

        internal static void Restore()
        {
            if (_eventsSubscribed)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
                _eventsSubscribed = false;
            }
            RestoreCameras();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            MainMenuManager menuManager = MainMenuManager.Get();
            string readinessSource;
            bool worldReady = IsWorldReady(menuManager, out readinessSource);
            Plugin.PluginLog.LogInfo("World scene loaded: name=" + scene.name
                + ", buildIndex=" + scene.buildIndex
                + ", mode=" + mode
                + ", worldReady=" + worldReady
                + ", source=" + readinessSource
                + ", activeCameras=" + Camera.allCameras.Length);
            LogCameraInventory("scene-loaded:" + scene.name);
        }

        private static void OnActiveSceneChanged(Scene previousScene, Scene activeScene)
        {
            Plugin.PluginLog.LogInfo("Active world scene changed: " + previousScene.name
                + " -> " + activeScene.name
                + ", activeCameras=" + Camera.allCameras.Length);
            LogCameraInventory("active-scene-changed:" + activeScene.name);
        }

        private static bool IsWorldReady(MainMenuManager menuManager, out string source)
        {
            if (menuManager != null
                && Traverse.Create(menuManager).Method("IsGameStarted").GetValue<bool>())
            {
                source = "menu-manager";
                return true;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (menuManager == null && activeScene.isLoaded
                && string.Equals(activeScene.name, "Level", StringComparison.Ordinal))
            {
                source = "active-game-scene";
                return true;
            }

            source = menuManager == null ? "no-menu-manager" : "menu-manager-not-started";
            return false;
        }

        private static void DisableReadyMenuCamera()
        {
            MainMenuManager menuManager = MainMenuManager.Get();
            if (menuManager == null
                || Traverse.Create(menuManager).Method("IsUIInputsBlocked").GetValue<bool>())
                return;

            Type activeScreen = Traverse.Create(menuManager)
                .Method("GetActiveScreen")
                .GetValue<Type>();
            if (activeScreen != typeof(MainMenu)
                && activeScreen != typeof(MainMenuJoinOrHost)
                && activeScreen != typeof(MainMenuChooseMode))
                return;

            Camera menuCamera = FindSceneCamera("Camera", "MainMenu");
            if (menuCamera == null || !menuCamera.enabled)
                return;

            if (!OriginalCameraStates.ContainsKey(menuCamera))
            {
                OriginalCameraStates.Add(menuCamera, menuCamera.enabled);
                Plugin.PluginLog.LogInfo("Disabling menu camera: " + menuCamera.name
                    + "; automatic menu handlers remain enabled.");
            }
            menuCamera.enabled = false;
        }

        private static bool IsCameraShutdownReady(out Camera gameplayCamera, out string reason)
        {
            Camera loadingCamera = FindSceneCamera("LoadingCamera", null);
            Camera inventoryCamera = FindSceneCamera("InventoryCamera", "Level");
            gameplayCamera = FindSceneCamera("Main Camera", "Level");

            bool loadingCameraActive = loadingCamera != null
                && loadingCamera.enabled
                && loadingCamera.gameObject.activeInHierarchy;
            bool inventoryCameraPresent = inventoryCamera != null;
            bool gameplayCameraActive = gameplayCamera != null
                && gameplayCamera.enabled
                && gameplayCamera.gameObject.activeInHierarchy;

            reason = "loadingCameraActive=" + loadingCameraActive
                + ", inventoryCameraPresent=" + inventoryCameraPresent
                + ", gameplayCameraActive=" + gameplayCameraActive;
            return !loadingCameraActive && inventoryCameraPresent && gameplayCameraActive;
        }

        private static Camera FindSceneCamera(string cameraName, string sceneName)
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            foreach (Camera camera in cameras)
            {
                if (camera == null || !camera.gameObject.scene.IsValid())
                    continue;
                if (!string.Equals(camera.name, cameraName, StringComparison.Ordinal))
                    continue;
                if (sceneName != null && !string.Equals(camera.gameObject.scene.name, sceneName, StringComparison.Ordinal))
                    continue;
                if (sceneName == null && camera.gameObject.scene.name != "Level"
                    && camera.gameObject.scene.name != "DontDestroyOnLoad")
                    continue;

                return camera;
            }
            return null;
        }

        private static void LogWorldStatus(MainMenuManager menuManager, bool worldReady, string readinessSource)
        {
            Camera[] cameras = Camera.allCameras;
            StringBuilder cameraNames = new StringBuilder();
            foreach (Camera camera in cameras)
            {
                if (camera == null)
                    continue;
                if (cameraNames.Length > 0)
                    cameraNames.Append(", ");
                cameraNames.Append(camera.name);
            }

            StringBuilder loadedScenes = new StringBuilder();
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded)
                    continue;
                if (loadedScenes.Length > 0)
                    loadedScenes.Append(", ");
                loadedScenes.Append(scene.name);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            Plugin.PluginLog.LogInfo("World status: worldReady=" + worldReady
                + ", source=" + readinessSource
                + ", managerPresent=" + (menuManager != null)
                + ", activeScene=" + activeScene.name
                + ", loadedScenes=[" + loadedScenes + "]"
                + ", enabledCameras=[" + cameraNames + "]"
                + ", focused=" + Application.isFocused
                + ", timeScale=" + Time.timeScale);
        }

        private static void LogCameraInventory(string reason)
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            int loggedCameras = 0;
            WriteCameraLog("Snapshot begin: reason=" + reason
                + ", activeScene=" + SceneManager.GetActiveScene().name
                + ", cameraCount=" + cameras.Length);

            foreach (Camera camera in cameras)
            {
                if (camera == null || !camera.gameObject.scene.IsValid())
                    continue;

                Scene scene = camera.gameObject.scene;
                WriteCameraLog("Camera: id=" + camera.GetInstanceID()
                    + ", name=" + camera.name
                    + ", type=" + camera.GetType().FullName
                    + ", scene=" + scene.name
                    + ", path=" + GetTransformPath(camera.transform)
                    + ", activeSelf=" + camera.gameObject.activeSelf
                    + ", activeInHierarchy=" + camera.gameObject.activeInHierarchy
                    + ", enabled=" + camera.enabled
                    + ", tag=" + camera.tag
                    + ", depth=" + camera.depth
                    + ", targetTexture=" + (camera.targetTexture == null ? "<none>" : camera.targetTexture.name));
                loggedCameras++;
            }

            WriteCameraLog("Snapshot end: reason=" + reason + ", sceneCameraCount=" + loggedCameras);
        }

        private static void WriteCameraLog(string message)
        {
            if (string.IsNullOrEmpty(_cameraLogPath))
                return;

            try
            {
                File.AppendAllText(_cameraLogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                    + " " + message + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("Could not write camera inventory log: " + exception.Message);
            }
        }

        private static string GetTransformPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static void RestoreCameras()
        {
            foreach (KeyValuePair<Camera, bool> cameraState in OriginalCameraStates)
            {
                if (cameraState.Key != null)
                    cameraState.Key.enabled = cameraState.Value;
            }
            OriginalCameraStates.Clear();
            _gameplayCamera = null;
            _cameraShutdownReadyLogged = false;
            _lastCameraReadiness = null;
        }
    }
}