using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GHServer
{
    internal static class GuideController
    {
        private sealed class Step { internal string Action; internal string Target; }
        private static readonly List<Step> Steps = new List<Step>();
        private static ConfigEntry<bool> _enabled;
        private static int _index;
        private static float _nextAttempt;

        internal static void Initialize(ConfigEntry<bool> enabled)
        {
            _enabled = enabled;
            string path = Path.Combine(Paths.ConfigPath, "guide.cfg");
            if (!File.Exists(path))
                return;
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string[] parts = rawLine.Split(new[] { ',' }, 2);
                if (parts.Length == 2 && (string.Equals(parts[0].Trim(), "click", StringComparison.OrdinalIgnoreCase)
                    || IsIndex(parts[0].Trim())))
                    Steps.Add(new Step { Action = parts[0].Trim().ToLowerInvariant(), Target = parts[1].Trim() });
            }
            Plugin.PluginLog.LogInfo("Loaded " + Steps.Count + " guide steps from " + path);
        }

        internal static void Update()
        {
            if (_enabled == null || !_enabled.Value || _index >= Steps.Count || Time.unscaledTime < _nextAttempt)
                return;
            _nextAttempt = Time.unscaledTime + 1.0f;
            Selectable[] controls = UnityEngine.Object.FindObjectsOfType<Selectable>();
            foreach (Selectable control in controls)
            {
                if (control == null || !control.gameObject.activeInHierarchy || !control.interactable)
                    continue;
                if (!Matches(control, Steps[_index].Target))
                    continue;
                PointerEventData pointer = new PointerEventData(EventSystem.current);
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(control.gameObject);
                int selectionIndex;
                if (int.TryParse(Steps[_index].Action, out selectionIndex))
                {
                    FieldInfo field = control.GetType().GetField("m_SelectionIdx",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null)
                        continue;
                    field.SetValue(control, selectionIndex);
                    TryRefresh(control);
                }
                else
                {
                    Button button = control as Button;
                    if (button == null)
                        continue;
                    ExecuteEvents.Execute<IPointerClickHandler>(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                }
                Plugin.PluginLog.LogInfo("Guide step " + (_index + 1) + "/" + Steps.Count + ": "
                    + Steps[_index].Action + " " + Steps[_index].Target);
                _index++;
                return;
            }
        }

        private static bool IsIndex(string value)
        {
            int ignored;
            return int.TryParse(value, out ignored);
        }

        private static void TryRefresh(Selectable control)
        {
            string[] methodNames = { "UpdateValue", "UpdateText", "Refresh", "SetValue" };
            foreach (string methodName in methodNames)
            {
                MethodInfo method = control.GetType().GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                if (method != null)
                    try { method.Invoke(control, null); } catch { }
            }
        }

        private static bool Matches(Selectable control, string target)
        {
            if (string.Equals(control.name, target, StringComparison.OrdinalIgnoreCase))
                return true;
            Text text = control.GetComponentInChildren<Text>(true);
            return text != null && string.Equals(text.text.Trim(), target, StringComparison.OrdinalIgnoreCase);
        }
    }
}
