using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenHellAutoStart
{
    internal static class UIEventFindings
    {
        private static string _path;
        private static readonly Dictionary<int, string> SelectorSnapshots = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> TextSnapshots = new Dictionary<int, string>();

        internal static void Initialize()
        {
            _path = Path.Combine(Paths.BepInExRootPath, "GreenHellAutoStart-UI-findings.log");
            try
            {
                File.WriteAllText(_path, "UI findings started " + DateTime.Now.ToString("O") + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("Could not initialize UI findings log: " + exception.Message);
            }

            PatchCustomSelectorMethods();
        }

        private static void PatchCustomSelectorMethods()
        {
            Type selectorType = typeof(MainMenuManager).Assembly.GetType("UISelectButton");
            if (selectorType == null)
                return;

            string[] methodNames = { "OnMove", "OnSubmit", "OnUpdateSelected", "OnKeyDown", "OnRight", "OnLeft" };
            Harmony harmony = new Harmony("local.greenhell.autostart.ui-findings");
            foreach (string methodName in methodNames)
            {
                MethodInfo method = selectorType.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                    continue;
                try
                {
                    harmony.Patch(method, prefix: new HarmonyMethod(
                        typeof(UIEventFindings).GetMethod("CustomSelectorPrefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                }
                catch (Exception exception)
                {
                    Plugin.PluginLog.LogWarning("Could not inspect UISelectButton." + methodName
                        + ": " + exception.Message);
                }
            }
        }

        private static void CustomSelectorPrefix(object __instance, MethodBase __originalMethod)
        {
            Component component = __instance as Component;
            if (component != null)
                Record("custom-" + __originalMethod.Name, component);
        }

        internal static void Record(string eventName, Component component)
        {
            if (component == null || string.IsNullOrEmpty(_path))
                return;
            try
            {
                Text label = component.GetComponentInChildren<Text>(true);
                string text = label == null ? "<no Text>" : label.text.Trim();
                File.AppendAllText(_path, DateTime.Now.ToString("O")
                    + " event=" + eventName
                    + " type=" + component.GetType().FullName
                    + " name=" + component.name
                    + " text=" + text
                    + " path=" + GetPath(component.transform)
                    + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("Could not write UI findings: " + exception.Message);
            }
        }

        internal static void Update()
        {
            WatchVisibilityText();
            Type selectorType = typeof(MainMenuManager).Assembly.GetType("UISelectButton");
            if (selectorType == null || string.IsNullOrEmpty(_path))
                return;

            UnityEngine.Object[] selectors = UnityEngine.Object.FindObjectsOfType(selectorType);
            foreach (UnityEngine.Object selector in selectors)
            {
                Component component = selector as Component;
                if (component == null)
                    continue;

                string snapshot = GetFieldSnapshot(selector, selectorType);
                int id = selector.GetInstanceID();
                string previous;
                if (!SelectorSnapshots.TryGetValue(id, out previous))
                {
                    SelectorSnapshots[id] = snapshot;
                    continue;
                }
                if (string.Equals(previous, snapshot, StringComparison.Ordinal))
                    continue;

                SelectorSnapshots[id] = snapshot;
                Write("event=field-change type=" + selectorType.FullName
                    + " name=" + component.name
                    + " path=" + GetPath(component.transform)
                    + " fields=" + snapshot);
            }
        }

        private static void WatchVisibilityText()
        {
            Text[] labels = UnityEngine.Object.FindObjectsOfType<Text>();
            foreach (Text label in labels)
            {
                if (label == null || !label.gameObject.activeInHierarchy)
                    continue;
                string path = GetPath(label.transform);
                string value = label.text == null ? "<null>" : label.text.Trim();
                string lowerValue = value.ToLowerInvariant();
                if (lowerValue.IndexOf("online visibility", StringComparison.Ordinal) < 0
                    && lowerValue.IndexOf("invite only", StringComparison.Ordinal) < 0
                    && lowerValue.IndexOf("friends only", StringComparison.Ordinal) < 0
                    && lowerValue.IndexOf("public", StringComparison.Ordinal) < 0)
                    continue;

                int id = label.GetInstanceID();
                string previous;
                if (!TextSnapshots.TryGetValue(id, out previous))
                {
                    TextSnapshots[id] = value;
                    Write("event=option-initial name=" + label.name + " option=" + lowerValue + " path=" + path);
                    continue;
                }
                if (string.Equals(previous, value, StringComparison.Ordinal))
                    continue;

                TextSnapshots[id] = value;
                Write("event=option-change name=" + label.name + " before=" + previous.ToLowerInvariant()
                    + " after=" + lowerValue + " path=" + path);
            }
        }

        private static string GetFieldSnapshot(object instance, Type type)
        {
            List<string> values = new List<string>();
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                FieldInfo[] fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string))
                    {
                        try { values.Add(field.Name + "=" + Convert.ToString(field.GetValue(instance))); }
                        catch { }
                    }
                }
            }
            return string.Join(";", values.ToArray());
        }

        private static void Write(string message)
        {
            try
            {
                File.AppendAllText(_path, DateTime.Now.ToString("O") + " " + message + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("Could not write UI findings: " + exception.Message);
            }
        }

        private static string GetPath(Transform transform)
        {
            string path = transform == null ? "<null>" : transform.name;
            while (transform != null && transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }

    [HarmonyPatch(typeof(Button), "OnPointerClick")]
    internal static class ButtonFindingsPatch
    {
        private static void Prefix(Button __instance) { UIEventFindings.Record("click", __instance); }
    }

    [HarmonyPatch(typeof(Toggle), "OnPointerClick")]
    internal static class ToggleFindingsPatch
    {
        private static void Prefix(Toggle __instance) { UIEventFindings.Record("toggle-click", __instance); }
    }

    [HarmonyPatch(typeof(Selectable), "OnPointerEnter")]
    internal static class SelectableHoverFindingsPatch
    {
        private static void Prefix(Selectable __instance) { UIEventFindings.Record("hover-enter", __instance); }
    }

    [HarmonyPatch(typeof(Selectable), "OnSelect")]
    internal static class SelectableFocusFindingsPatch
    {
        private static void Prefix(Selectable __instance, BaseEventData eventData)
        {
            UIEventFindings.Record("focus", __instance);
        }
    }

    [HarmonyPatch(typeof(Selectable), "OnDeselect")]
    internal static class SelectableDeselectFindingsPatch
    {
        private static void Prefix(Selectable __instance, BaseEventData eventData)
        {
            UIEventFindings.Record("deselect", __instance);
        }
    }

    [HarmonyPatch(typeof(Button), "OnSubmit")]
    internal static class ButtonSubmitFindingsPatch
    {
        private static void Prefix(Button __instance, BaseEventData eventData)
        {
            UIEventFindings.Record("submit", __instance);
        }
    }

    [HarmonyPatch(typeof(Selectable), "OnPointerDown")]
    internal static class SelectablePointerDownFindingsPatch
    {
        private static void Prefix(Selectable __instance, PointerEventData eventData)
        {
            UIEventFindings.Record("pointer-down", __instance);
        }
    }

    [HarmonyPatch(typeof(Selectable), "OnPointerUp")]
    internal static class SelectablePointerUpFindingsPatch
    {
        private static void Prefix(Selectable __instance, PointerEventData eventData)
        {
            UIEventFindings.Record("pointer-up", __instance);
        }
    }
}
