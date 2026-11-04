using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace GHServer
{
    internal static class MemoryFindings
    {
        private static ConfigEntry<bool> _enabled;
        private static ConfigEntry<float> _interval;
        private static string _path;
        private static float _next;

        internal static void Initialize(ConfigFile config)
        {
            _enabled = config.Bind("Diagnostics", "MemoryFindings", false,
                "Write periodic Unity memory and loaded-object summaries to GHServer-MemoryFindings.log.");
            _interval = config.Bind("Diagnostics", "MemoryFindingsIntervalSeconds", 30f,
                "Seconds between memory snapshots.");

            if (!_enabled.Value)
                return;

            _path = Path.Combine(Paths.BepInExRootPath, "GHServer-MemoryFindings.log");
            try
            {
                File.WriteAllText(_path, "Memory findings started " + DateTime.Now.ToString("O") + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("Could not initialize memory findings log: " + exception.Message);
            }
        }

        internal static void Update()
        {
            if (_enabled == null || !_enabled.Value || string.IsNullOrEmpty(_path)
                || Time.unscaledTime < _next)
                return;

            _next = Time.unscaledTime + Mathf.Clamp(_interval.Value, 5f, 3600f);
            WriteSnapshot();
        }

        private static void WriteSnapshot()
        {
            try
            {
                UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
                int activeCanvases = 0;
                foreach (Canvas canvas in canvases)
                    if (canvas != null && canvas.isActiveAndEnabled)
                        activeCanvases++;
                Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (UnityEngine.Object instance in objects)
                {
                    if (instance == null)
                        continue;
                    string typeName = instance.GetType().FullName;
                    int count;
                    counts.TryGetValue(typeName, out count);
                    counts[typeName] = count + 1;
                }

                List<KeyValuePair<string, int>> sorted = new List<KeyValuePair<string, int>>(counts);
                sorted.Sort((left, right) => right.Value.CompareTo(left.Value));

                string line = DateTime.Now.ToString("O")
                    + " scene=" + SceneManager.GetActiveScene().name
                    + " scenesLoaded=" + SceneManager.sceneCount
                    + " unityAllocated=" + FormatBytes(Profiler.GetTotalAllocatedMemoryLong())
                    + " unityReserved=" + FormatBytes(Profiler.GetTotalReservedMemoryLong())
                    + " unityUnusedReserved=" + FormatBytes(Profiler.GetTotalUnusedReservedMemoryLong())
                    + " monoUsed=" + FormatBytes(Profiler.GetMonoUsedSizeLong())
                    + " monoHeap=" + FormatBytes(Profiler.GetMonoHeapSizeLong())
                    + " managed=" + FormatBytes(GC.GetTotalMemory(false))
                    + " graphicsDevice=" + SystemInfo.graphicsDeviceName
                    + " graphicsDeviceMemoryMB=" + SystemInfo.graphicsMemorySize
                    + " canvases=" + canvases.Length
                    + " activeCanvases=" + activeCanvases
                    + " renderers=" + renderers.Length
                    + " loadedObjects=" + objects.Length;
                Append(line + Environment.NewLine);

                int limit = Math.Min(sorted.Count, 40);
                for (int index = 0; index < limit; index++)
                    Append("  type=" + sorted[index].Key + " count=" + sorted[index].Value + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("Could not write memory findings: " + exception.Message);
            }
        }

        private static string FormatBytes(long bytes)
        {
            return (bytes / (1024d * 1024d)).ToString("F1") + "MB (" + bytes + " bytes)";
        }

        private static void Append(string text)
        {
            File.AppendAllText(_path, text);
        }
    }
}
