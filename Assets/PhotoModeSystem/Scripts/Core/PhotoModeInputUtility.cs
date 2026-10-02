using System;
using UnityEngine;

namespace PhotoModeSystem
{
    public static class PhotoModeInputUtility
    {
        private static Type keyboardType;
        private static bool isReflectionChecked;

        private static void EnsureInitialized()
        {
            if (isReflectionChecked) return;
            isReflectionChecked = true;
            keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
        }

        public static bool IsEnterPressed()
        {
            EnsureInitialized();

            // 1. Try Unity New Input System via Reflection
            if (keyboardType != null)
            {
                try
                {
                    var currentProp = keyboardType.GetProperty("current");
                    var currentKeyboard = currentProp?.GetValue(null);
                    if (currentKeyboard != null)
                    {
                        var enterControl = keyboardType.GetProperty("enterKey")?.GetValue(currentKeyboard);
                        var numpadEnterControl = keyboardType.GetProperty("numpadEnterKey")?.GetValue(currentKeyboard);

                        if (IsControlPressedThisFrame(enterControl) || IsControlPressedThisFrame(numpadEnterControl))
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }

            // 2. Try Legacy Input Manager
            try
            {
                return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            }
            catch (InvalidOperationException)
            {
                // Silently ignore if legacy input handling is disabled in Player Settings
                return false;
            }
            catch { }

            return false;
        }

        public static bool IsTabPressed()
        {
            EnsureInitialized();

            // 1. Try Unity New Input System via Reflection
            if (keyboardType != null)
            {
                try
                {
                    var currentProp = keyboardType.GetProperty("current");
                    var currentKeyboard = currentProp?.GetValue(null);
                    if (currentKeyboard != null)
                    {
                        var tabControl = keyboardType.GetProperty("tabKey")?.GetValue(currentKeyboard);

                        if (IsControlPressedThisFrame(tabControl))
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }

            // 2. Try Legacy Input Manager
            try
            {
                return Input.GetKeyDown(KeyCode.Tab);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch { }

            return false;
        }

        private static bool IsControlPressedThisFrame(object buttonControl)
        {
            if (buttonControl == null) return false;
            try
            {
                var wasPressedProp = buttonControl.GetType().GetProperty("wasPressedThisFrame");
                if (wasPressedProp != null)
                {
                    return (bool)wasPressedProp.GetValue(buttonControl);
                }
            }
            catch { }
            return false;
        }
    }
}
