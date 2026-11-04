using System;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GreenHellAutoStart
{
    internal static class ContextFindings
    {
        private static string _path;
        private static float _next;
        private static string _lastSnapshot;

        internal static void Initialize()
        {
            _path = Path.Combine(Paths.BepInExRootPath, "GreenHellAutoStart-ContextFindings.log");
            try { File.WriteAllText(_path, "Context findings started " + DateTime.Now.ToString("O") + Environment.NewLine); }
            catch (Exception exception) { Plugin.PluginLog.LogError("Could not initialize context log: " + exception.Message); }
        }

        internal static void Update()
        {
            if (string.IsNullOrEmpty(_path) || Time.unscaledTime < _next)
                return;
            _next = Time.unscaledTime + 0.5f;

            string snapshot = "screen=" + GetActiveScreen();
            GameObject selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            snapshot += " selected=" + (selected == null ? "<none>" : GetPath(selected.transform));

            Component[] selectors = UnityEngine.Object.FindObjectsOfType<Component>();
            foreach (Component component in selectors)
            {
                if (component == null || component.name.IndexOf("Visibility", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                snapshot += " object=" + component.GetType().FullName + "(" + component.name + ") fields="
                    + GetFields(component);
            }

            if (snapshot == _lastSnapshot)
                return;
            _lastSnapshot = snapshot;
            try { File.AppendAllText(_path, DateTime.Now.ToString("O") + " " + snapshot + Environment.NewLine); }
            catch (Exception exception) { Plugin.PluginLog.LogError("Could not write context log: " + exception.Message); }
        }

        private static string GetActiveScreen()
        {
            MainMenuManager manager = MainMenuManager.Get();
            if (manager == null)
                return "<none>";
            Type screen = Traverse.Create(manager).Method("GetActiveScreen").GetValue<Type>();
            return screen == null ? "<none>" : screen.FullName;
        }

        private static string GetFields(object instance)
        {
            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
            for (Type type = instance.GetType(); type != null && type != typeof(object); type = type.BaseType)
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string))
                        try { result.Add(field.Name + "=" + Convert.ToString(field.GetValue(instance))); } catch { }
            return string.Join(";", result.ToArray());
        }

        private static string GetPath(Transform transform)
        {
            string path = transform == null ? "<null>" : transform.name;
            while (transform != null && transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
            return path;
        }
    }
}
