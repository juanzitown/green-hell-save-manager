using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly Dictionary<Camera, bool> OriginalCameraStates = new Dictionary<Camera, bool>();
        private static readonly Dictionary<int, string> CameraInventory = new Dictionary<int, string>();
        private static readonly HashSet<int> LiveCameras = new HashSet<int>();
        private static readonly HashSet<string> CameraNamesToKeep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _eventsSubscribed;
        private static float _nextCameraScanTime;
        private static string _cameraLogPath;

        internal static void Initialize(ConfigFile config)
        {
            _disableCameraRendering = config.Bind("Graphics", "DisableCameraRendering", true,
                "Disable each discovered scene camera unless its name or path appears in CameraNamesToKeep.");
            CameraNamesToKeep.Clear();
            CameraNamesToKeep.Add("Main Camera");


            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            _eventsSubscribed = true;
        }

        internal static void Update()
        {
            if (Time.unscaledTime < _nextCameraScanTime)
                return;
            _nextCameraScanTime = Time.unscaledTime + 1.0f;
            ScanCameras();
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
            ScanCameras();
        }

        private static void OnActiveSceneChanged(Scene previousScene, Scene activeScene)
        {
            ScanCameras();
        }

        private static void ScanCameras()
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            HashSet<int> seenCameras = new HashSet<int>();
            bool loadingScreenActive = IsLoadingScreenActive(cameras);
            bool worldStarted = IsWorldStarted();
            string phase = loadingScreenActive ? "loading" : worldStarted ? "world" : "menu";

            foreach (Camera camera in cameras)
            {
                if (camera == null || !camera.gameObject.scene.IsValid())
                    continue;

                int instanceId = camera.GetInstanceID();
                string details = GetCameraDetails(camera);
                seenCameras.Add(instanceId);

                string previousDetails;
                bool isNewCamera = !CameraInventory.TryGetValue(instanceId, out previousDetails);
                bool isLive = camera.enabled && camera.gameObject.activeInHierarchy;
                bool becameLive = isLive && LiveCameras.Add(instanceId);
                bool becameInactive = !isLive && LiveCameras.Remove(instanceId);
                bool isKeptDuringLoading = loadingScreenActive && IsCameraKept(camera);

                if (false && becameLive)
                {
                    WriteCameraLog((isKeptDuringLoading ? "KEEP LIVE " : "LIVE ")
                        + "phase=" + phase + " " + details);
                }
                else if (isLive && !isNewCamera && !string.Equals(previousDetails, details, StringComparison.Ordinal)
                    && false)
                {
                    WriteCameraLog("CHANGE " + details);
                }
                else if (becameInactive && false)
                {
                    WriteCameraLog("INACTIVE " + details);
                }
                CameraInventory[instanceId] = details;

                bool shouldDisable = _disableCameraRendering.Value
                    && (loadingScreenActive
                        ? !isKeptDuringLoading
                        : worldStarted);
                if (shouldDisable)
                    DisableCamera(camera, details, phase);
                else
                    RestoreCamera(camera);
            }

            List<int> removedCameras = new List<int>();
            foreach (KeyValuePair<int, string> camera in CameraInventory)
            {
                if (!seenCameras.Contains(camera.Key))
                {
                    if (false)
                        WriteCameraLog("REMOVE " + camera.Value);
                    LiveCameras.Remove(camera.Key);
                    removedCameras.Add(camera.Key);
                }
            }
            foreach (int instanceId in removedCameras)
                CameraInventory.Remove(instanceId);
        }

        private static bool IsCameraKept(Camera camera)
        {
            return CameraNamesToKeep.Contains(camera.name)
                || CameraNamesToKeep.Contains(GetTransformPath(camera.transform));
        }

        private static bool IsLoadingScreenActive(Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                if (camera != null
                    && camera.gameObject.scene.IsValid()
                    && camera.name == "LoadingCamera"
                    && camera.gameObject.activeInHierarchy)
                    return true;
            }
            return false;
        }

        private static bool IsWorldStarted()
        {
            MainMenuManager menuManager = MainMenuManager.Get();
            if (menuManager != null
                && Traverse.Create(menuManager).Method("IsGameStarted").GetValue<bool>())
                return true;

            Scene activeScene = SceneManager.GetActiveScene();
            return menuManager == null && activeScene.isLoaded
                && activeScene.name == "Level";
        }

        private static void DisableCamera(Camera camera, string details, string phase)
        {
            if (!OriginalCameraStates.ContainsKey(camera))
                OriginalCameraStates.Add(camera, camera.enabled);

            if (!camera.enabled)
                return;

            camera.enabled = false;
        }

        private static void RestoreCamera(Camera camera)
        {
            bool originalState;
            if (!OriginalCameraStates.TryGetValue(camera, out originalState))
                return;

            camera.enabled = originalState;
            OriginalCameraStates.Remove(camera);
        }

        private static string GetCameraDetails(Camera camera)
        {
            Scene scene = camera.gameObject.scene;
            return "depth=" + camera.depth
                + ", scene=" + scene.name
                + ", name=" + camera.name
                + ", path=" + GetTransformPath(camera.transform);
        }

        private static void WriteCameraLog(string message)
        {
            if (string.IsNullOrEmpty(_cameraLogPath))
                return;

            try
            {
                File.AppendAllText(_cameraLogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine);
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
            CameraInventory.Clear();
            LiveCameras.Clear();
        }
    }
}
