using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GreenHellAutoStart
{
    internal static class UIUtils
    {
        private static readonly HashSet<int> ObservedButtons = new HashSet<int>();
        private static Type _lastActiveScreen;
        private static bool _hasLoggedScreen;
        private static bool _diagnosticsStarted;
        private static bool _diagnosticsFailed;

        internal static void LogUnityClick(string eventName, Button button)
        {
            if (Plugin.LogMenuEvents == null || !Plugin.LogMenuEvents.Value || button == null)
                return;

            try
            {
                string screenName = _lastActiveScreen == null ? "<unknown>" : _lastActiveScreen.Name;
                Plugin.PluginLog.LogInfo("UI click: event=" + eventName
                    + ", screen=" + screenName
                    + ", type=" + button.GetType().FullName
                    + ", name=" + GetButtonName(button)
                    + ", path=" + GetTransformPath(button.transform));

                int listenerCount = button.onClick.GetPersistentEventCount();
                Plugin.PluginLog.LogInfo("UI click persistent listeners: " + listenerCount);
                for (int index = 0; index < listenerCount; index++)
                {
                    UnityEngine.Object target = button.onClick.GetPersistentTarget(index);
                    string targetType = target == null ? "<none>" : target.GetType().FullName;
                    Plugin.PluginLog.LogInfo("UI click listener: targetType=" + targetType
                        + ", method=" + button.onClick.GetPersistentMethodName(index));
                    LogInstanceFields(targetType, target);
                }

                Component[] components = button.GetComponents<Component>();
                foreach (Component component in components)
                {
                    if (component != null && component is MonoBehaviour)
                        LogInstanceFields(component.GetType().FullName, component);
                }
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("UI click logging failed: " + exception);
            }
        }

        internal static void LogContinueHandler(string handlerName, ContinueButtonLogic continueLogic)
        {
            if (Plugin.LogMenuEvents == null || !Plugin.LogMenuEvents.Value)
                return;

            try
            {
                Button button = continueLogic == null
                    ? null
                    : Traverse.Create(continueLogic).Field("m_Button").GetValue<Button>();
                object parentMenu = continueLogic == null
                    ? null
                    : Traverse.Create(continueLogic).Field("m_ParentUI").GetValue();
                object saveInfo = continueLogic == null
                    ? null
                    : Traverse.Create(continueLogic).Field("m_LastSavedGameInfo").GetValue();

                string screenName = _lastActiveScreen == null ? "<unknown>" : _lastActiveScreen.Name;
                string parentName = parentMenu == null ? "<unknown>" : parentMenu.GetType().Name;
                string buttonName = GetButtonName(button);
                Plugin.PluginLog.LogInfo("Continue handler invoked: handler=" + handlerName
                    + ", screen=" + screenName
                    + ", parent=" + parentName
                    + ", button=" + buttonName
                    + ", saveAvailable=" + (saveInfo != null));
            }
            catch (Exception exception)
            {
                Plugin.PluginLog.LogError("Continue handler logging failed: " + exception);
            }
        }

        internal static void CaptureMenuEvents(MainMenuManager manager)
        {
            if (_diagnosticsFailed)
                return;

            try
            {
                if (!_diagnosticsStarted)
                {
                    Plugin.PluginLog.LogInfo("Menu diagnostics active; automatic navigation remains enabled.");
                    _diagnosticsStarted = true;
                }

                Type activeScreen = Traverse.Create(manager)
                    .Method("GetActiveScreen")
                    .GetValue<Type>();
                if (!_hasLoggedScreen || activeScreen != _lastActiveScreen)
                {
                    string screenName = activeScreen == null ? "<none>" : activeScreen.Name;
                    Plugin.PluginLog.LogInfo("Active menu screen: " + screenName);
                    _lastActiveScreen = activeScreen;
                    _hasLoggedScreen = true;
                }

                Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>();
                foreach (Button button in buttons)
                {
                    if (button == null || !ObservedButtons.Add(button.GetInstanceID()))
                        continue;

                    Button observedButton = button;
                    observedButton.onClick.AddListener(delegate { LogButtonClick(observedButton); });
                }
            }
            catch (Exception exception)
            {
                _diagnosticsFailed = true;
                Plugin.PluginLog.LogError("Menu diagnostics failed: " + exception);
            }
        }

        private static void LogButtonClick(Button button)
        {
            LogUnityClick("onClick", button);
        }

        private static string GetButtonName(Button button)
        {
            if (button == null)
                return "<unavailable>";

            Text label = button.GetComponentInChildren<Text>(true);
            return label != null && !string.IsNullOrEmpty(label.text)
                ? label.text.Trim()
                : button.name;
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

        private static void LogInstanceFields(string objectType, object instance)
        {
            if (instance == null)
                return;

            Plugin.PluginLog.LogInfo("UI object: type=" + objectType);
            Type currentType = instance.GetType();
            int fieldCount = 0;
            while (currentType != null && currentType != typeof(object)
                && currentType != typeof(MonoBehaviour)
                && currentType != typeof(Component)
                && currentType != typeof(UnityEngine.Object)
                && fieldCount < 20)
            {
                FieldInfo[] fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    if (field.IsStatic || fieldCount >= 20)
                        continue;

                    string value;
                    try
                    {
                        value = FormatFieldValue(field.GetValue(instance));
                    }
                    catch
                    {
                        value = "<unavailable>";
                    }

                    Plugin.PluginLog.LogInfo("UI field: " + objectType + "." + field.Name
                        + " (" + field.FieldType.Name + ")=" + value);
                    fieldCount++;
                }
                currentType = currentType.BaseType;
            }
        }

        private static string FormatFieldValue(object value)
        {
            if (value == null)
                return "<null>";

            Type valueType = value.GetType();
            if (value is string || valueType.IsPrimitive || valueType.IsEnum || value is decimal)
            {
                string text = Convert.ToString(value);
                return text.Length <= 100 ? text : text.Substring(0, 100);
            }

            UnityEngine.Object unityObject = value as UnityEngine.Object;
            if (!ReferenceEquals(unityObject, null))
                return unityObject == null ? "<destroyed>" : unityObject.GetType().Name + "(" + unityObject.name + ")";

            ICollection collection = value as ICollection;
            if (collection != null)
                return valueType.Name + "(count=" + collection.Count + ")";

            Delegate callback = value as Delegate;
            if (callback != null)
                return "Delegate(" + callback.Method.DeclaringType.Name + "." + callback.Method.Name + ")";

            return valueType.FullName;
        }
    }

    [HarmonyPatch(typeof(MainMenuChooseMode), "OnContinue")]
    internal static class ChooseModeContinueDiagnosticsPatch
    {
        [HarmonyPrefix]
        private static void Prefix(MainMenuChooseMode __instance)
        {
            ContinueButtonLogic continueLogic = Traverse.Create(__instance)
                .Field("m_ContinueLogic")
                .GetValue<ContinueButtonLogic>();
            UIUtils.LogContinueHandler("MainMenuChooseMode.OnContinue", continueLogic);
        }
    }

    [HarmonyPatch(typeof(ContinueButtonLogic), "OnButtonPressed")]
    internal static class ContinueButtonDiagnosticsPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ContinueButtonLogic __instance)
        {
            UIUtils.LogContinueHandler("ContinueButtonLogic.OnButtonPressed", __instance);
        }
    }

    [HarmonyPatch(typeof(Button), "OnPointerClick")]
    internal static class ButtonPointerClickDiagnosticsPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Button __instance)
        {
            UIUtils.LogUnityClick("OnPointerClick", __instance);
        }
    }

    [HarmonyPatch(typeof(Button), "OnSubmit")]
    internal static class ButtonSubmitDiagnosticsPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Button __instance)
        {
            UIUtils.LogUnityClick("OnSubmit", __instance);
        }
    }
}