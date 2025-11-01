using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using UnityExplorer;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib.Input;
#if UNHOLLOWER
using IL2CPPUtils = UnhollowerBaseLib.UnhollowerUtils;
#endif
#if INTEROP
using IL2CPPUtils = Il2CppInterop.Common.Il2CppInteropUtils;
#endif

namespace UniverseLib.Input
{
    public static class IInputManager {
        // TODO: Refactor this to have the input system used on the game saved in a variable,
        // and just call the methods of this variable in each function, like a proper wrapper.
        private static InputType currentInputType;

        public static void Setup(){
            currentInputType = InputManager.CurrentType;

            switch(currentInputType){
                case InputType.Legacy:
                    ILegacyInput.Init();
                    break;
                case InputType.InputSystem:
                    INewInputSystem.Init();
                    break;
                case InputType.None:
                    break;
            }
        }

        public static bool GetKey(KeyCode key){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetKey(key);
                case InputType.InputSystem:
                    return INewInputSystem.GetKey(key);
                case InputType.None:
                default:
                    return InputManager.GetKey(key);
            }
        }

        public static bool GetKeyDown(KeyCode key){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetKeyDown(key);
                case InputType.InputSystem:
                    string buttonName = key.ToString();
                    return INewInputSystem.GetButtonWasPressed($"/Keyboard/{buttonName}");
                case InputType.None:
                default:
                    return InputManager.GetKeyDown(key);
            }
        }

        public static bool GetKeyUp(KeyCode key){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetKeyUp(key);
                case InputType.InputSystem:
                    // Closest equivalent in the new input system is "wasPressedThisFrame"?
                    string buttonName = $"/Keyboard/{key.ToString()}";
                    return INewInputSystem.GetButtonWasPressed(buttonName);
                case InputType.None:
                default:
                    return InputManager.GetKeyUp(key);
            }
        }

        public static bool GetMouseButton(int button){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetMouseButton(button);
                case InputType.InputSystem:
                    return INewInputSystem.GetMouseButton(button);
                case InputType.None:
                default:
                    return InputManager.GetMouseButton(button);
            }
        }

        public static bool GetMouseButtonDown(int button){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetMouseButtonDown(button);
                case InputType.InputSystem:
                    string buttonName = button switch
                    {
                        0 => "/Mouse/leftButton",
                        1 => "/Mouse/rightButton",
                        2 => "/Mouse/middleButton",
                        _ => $"/Mouse/button{button}"
                    };
                    return INewInputSystem.GetButtonWasPressed(buttonName);
                case InputType.None:
                default:
                    return InputManager.GetMouseButtonDown(button);
            }
        }

        // We won't mock this as it made it impossible to drag the mod panels across the screen in some games for some reason.
        // It might affect some games that use custom classes to control the camera, but those would still probably need to be
        // manually disabled because of the camera position control override. Should try it out with more games.
        public static Vector3 MousePosition => (Vector3)InputManager.MousePosition;
    }

    public static class ILegacyInput {
        public static Dictionary<KeyCode, bool> getKeyDict = new Dictionary<KeyCode, bool>();
        public static Dictionary<KeyCode, bool> getKeyDownDict = new Dictionary<KeyCode, bool>();
        public static Dictionary<KeyCode, bool> getKeyUpDict = new Dictionary<KeyCode, bool>();

        public static Dictionary<int, bool> getMouseButton = new Dictionary<int, bool>();
        public static Dictionary<int, bool> getMouseButtonDown = new Dictionary<int, bool>();
        
        // Wrapped methods

        public static bool GetKey(KeyCode key){
            if (key == KeyCode.None) return false;
            // Trigger the original InputManager method
            InputManager.GetKey(key);
            return getKeyDict[key];
        }

        public static bool GetKeyDown(KeyCode key){
            if (key == KeyCode.None) return false;
            // Trigger the original InputManager method
            InputManager.GetKeyDown(key);
            return getKeyDownDict[key];
        }

        public static bool GetKeyUp(KeyCode key){
            if (key == KeyCode.None) return false;
            // Trigger the original InputManager method
            InputManager.GetKeyUp(key);
            return getKeyUpDict[key];
        }

        public static bool GetMouseButton(int button){
            // Trigger the original InputManager method
            InputManager.GetMouseButton(button);
            return getMouseButton[button];
        }

        public static bool GetMouseButtonDown(int button){
            // Trigger the original InputManager method
            InputManager.GetMouseButtonDown(button);
            return getMouseButtonDown[button];
        }

        // Patch the input methods of the legacy input system
        public static void Init(){
            Type t_Input = ReflectionUtility.GetTypeByName("UnityEngine.Input");

            try
            {
                MethodInfo getKeyTarget = t_Input.GetMethod("GetKey", new Type[] {typeof(string)});
                //ExplorerCore.LogWarning(getKeyTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyString))));
            }
            catch { }

            try
            {
                MethodInfo getKeyTarget = t_Input.GetMethod("GetKey", new Type[] {typeof(KeyCode)});
                //ExplorerCore.LogWarning(getKeyTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyKeyCode))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyDownTarget = t_Input.GetMethod("GetKeyDown", new Type[] {typeof(string)});
                //ExplorerCore.LogWarning(getKeyDownTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyDownTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyDownTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyDownString))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyDownTarget = t_Input.GetMethod("GetKeyDown", new Type[] {typeof(KeyCode)});
                //ExplorerCore.LogWarning(getKeyDownTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyDownTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyDownTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyDownKeyCode))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyUpTarget = t_Input.GetMethod("GetKeyUp", new Type[] {typeof(string)});
                //ExplorerCore.LogWarning(getKeyUpTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyUpTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyUpTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyUpString))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyUpTarget = t_Input.GetMethod("GetKeyUp", new Type[] {typeof(KeyCode)});
                //ExplorerCore.LogWarning(getKeyUpTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyUpTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyUpTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyUpKeyCode))));
            }
            catch {  }

            try
            {
                MethodInfo getMouseButtonTarget = t_Input.GetMethod("GetMouseButton", new Type[] {typeof(int)});
                //ExplorerCore.LogWarning(getMouseButtonTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getMouseButtonTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getMouseButtonTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideMouseButton))));
            }
            catch {  }

            try
            {
                MethodInfo getMouseButtonDownTarget = t_Input.GetMethod("GetMouseButtonDown", new Type[] {typeof(int)});
                //ExplorerCore.LogWarning(getMouseButtonDownTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getMouseButtonDownTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getMouseButtonDownTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideMouseButtonDown))));
            }
            catch {  }
        }

        // Postfix functions

        public static void OverrideKeyString(ref bool __result, ref string name)
        {
            KeyCode thisKeyCode = (KeyCode) System.Enum.Parse(typeof(KeyCode), name);
            getKeyDict[thisKeyCode] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyKeyCode(ref bool __result, ref KeyCode key)
        {
            if (key == KeyCode.None) return;

            getKeyDict[key] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyDownString(ref bool __result, ref string name)
        {
            KeyCode thisKeyCode = (KeyCode) System.Enum.Parse(typeof(KeyCode), name);
            getKeyDownDict[thisKeyCode] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyDownKeyCode(ref bool __result, ref KeyCode key)
        {
            if (key == KeyCode.None) return;

            getKeyDownDict[key] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyUpString(ref bool __result, ref string name)
        {
            KeyCode thisKeyCode = (KeyCode) System.Enum.Parse(typeof(KeyCode), name);
            getKeyUpDict[thisKeyCode] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyUpKeyCode(ref bool __result, ref KeyCode key)
        {
            if (key == KeyCode.None) return;

            getKeyUpDict[key] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideMouseButton(ref bool __result, ref int button)
        {
            getMouseButton[button] = __result;
            // Since CinematicUnityExplorer uses Unity's native UI for its menu, we can't switch off the mouse interaction with it on this wrapper.
            // Therefore, if we still want to interact with the Unity Explorer menu we would need to let the button action pass through when it's open.
            if (FreeCamPanel.ShouldOverrideInput() && !(button == 0 && UIManager.ShowMenu)){
                __result = false;
            }
        }

        public static void OverrideMouseButtonDown(ref bool __result, ref int button)
        {
            getMouseButtonDown[button] = __result;
            // Since CinematicUnityExplorer uses Unity's native UI for its menu, we can't switch off the mouse interaction with it on this wrapper.
            // Therefore, if we still want to interact with the Unity Explorer menu we would need to let the button action pass through when it's open.
            if (FreeCamPanel.ShouldOverrideInput() && !(button == 0 && UIManager.ShowMenu)){
                __result = false;
            }
        }
    }

    public static class INewInputSystem
    {
        private static MethodInfo isValueConsideredPressed;
        private static PropertyInfo isPressedProp;
        private static PropertyInfo wasPressedProp;
        private static MethodInfo isPressedMethod;
        private static MethodInfo isInProgressMethod;
        private static MethodInfo wasPressedMethod;
        private static MethodInfo wasPerformedMethod;

        private static Dictionary<string, object> buttonControls = new();

        public static void Init()
        {
            Type buttonControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem");
            if (buttonControlType != null)
            {
                try
                {
                    isValueConsideredPressed = buttonControlType.GetMethod("IsValueConsideredPressed");
                    if (isValueConsideredPressed != null)
                    {
#if CPP
                        if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(isValueConsideredPressed) == null)
                            throw new Exception();
#endif
                        ExplorerCore.Harmony.Patch(isValueConsideredPressed,
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Prefix))),
                            postfix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Postfix))));
                    }
                }
                catch (Exception ex) 
                { 
                    ExplorerCore.LogWarning($"Failed to patch IsValueConsideredPressed: {ex.Message}");
                }

                try
                {
                    isPressedProp = buttonControlType.GetProperty("isPressed");
                    if (isPressedProp?.GetGetMethod() != null)
                    {
#if CPP
                        if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(isPressedProp.GetGetMethod()) == null)
                            throw new Exception();
#endif
                        ExplorerCore.Harmony.Patch(isPressedProp.GetGetMethod(),
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Prefix))),
                            postfix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Postfix))));
                    }
                }
                catch (Exception ex) 
                { 
                    ExplorerCore.LogWarning($"Failed to patch isPressed: {ex.Message}");
                }

                try
                {
                    wasPressedProp = buttonControlType.GetProperty("wasPressedThisFrame");
                    if (wasPressedProp?.GetGetMethod() != null)
                    {
#if CPP
                        if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(wasPressedProp.GetGetMethod()) == null)
                            throw new Exception();
#endif
                        ExplorerCore.Harmony.Patch(wasPressedProp.GetGetMethod(),
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Prefix))),
                            postfix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Postfix))));
                    }
                }
                catch (Exception ex) 
                { 
                    ExplorerCore.LogWarning($"Failed to patch wasPressedThisFrame: {ex.Message}");
                }
            }

            // Patch InputAction methods
            Type inputActionType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputAction, Unity.InputSystem");
            if (inputActionType != null)
            {
                try
                {
                    isPressedMethod = inputActionType.GetMethod("IsPressed");
                    if (isPressedMethod != null)
                    {
#if CPP
                        if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(isPressedMethod) == null)
                            throw new Exception();
#endif
                        ExplorerCore.Harmony.Patch(isPressedMethod,
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Prefix))),
                            postfix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Postfix))));
                    }
                }
                catch (Exception ex) 
                { 
                    ExplorerCore.LogWarning($"Failed to patch IsPressed: {ex.Message}");
                }

                try
                {
                    ExplorerCore.LogWarning("Attempting to patch InputAction.IsInProgress...");
                    isInProgressMethod = inputActionType.GetMethod("IsInProgress");
                    if (isInProgressMethod != null)
                    {
                        ExplorerCore.LogWarning("Found InputAction.IsInProgress method.");
#if CPP
                        if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(isInProgressMethod) == null)
                            throw new Exception();
#endif
                        ExplorerCore.Harmony.Patch(isInProgressMethod,
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Prefix))),
                            postfix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Postfix))));
                    }
                }
                catch (Exception ex) 
                { 
                    ExplorerCore.LogWarning($"Failed to patch IsInProgress: {ex.Message}");
                }

                try
                {
                    ExplorerCore.LogWarning("Attempting to patch InputAction.WasPressedThisFrame...");
                    wasPressedMethod = inputActionType.GetMethod("WasPressedThisFrame");
                    if (wasPressedMethod != null)
                    {
                        ExplorerCore.LogWarning("Found InputAction.WasPressedThisFrame method.");
#if CPP
                        if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(wasPressedMethod) == null)
                            throw new Exception();
#endif
                        ExplorerCore.Harmony.Patch(wasPressedMethod,
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Prefix))),
                            postfix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Postfix))));
                    }
                }
                catch (Exception ex) 
                { 
                    ExplorerCore.LogWarning($"Failed to patch WasPressedThisFrame: {ex.Message}");
                }

                try
                {
                    ExplorerCore.LogWarning("Attempting to patch InputAction.WasPerformedThisFrame...");
                    wasPerformedMethod = inputActionType.GetMethod("WasPerformedThisFrame");
                    if (wasPerformedMethod != null)
                    {
                        ExplorerCore.LogWarning("Found InputAction.WasPerformedThisFrame method.");
#if CPP
                        if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(wasPerformedMethod) == null)
                            throw new Exception();
#endif
                        ExplorerCore.Harmony.Patch(wasPerformedMethod,
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Prefix))),
                            postfix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), nameof(Postfix))));
                    }
                }
                catch (Exception ex) 
                { 
                    ExplorerCore.LogWarning($"Failed to patch WasPerformedThisFrame: {ex.Message}");
                }
            }

            getButtonControls();
        }

        public static void getButtonControls()
        {
            var buttonControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem");
            var keyControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.KeyControl, Unity.InputSystem");
            var pathProp = buttonControlType?.GetProperty("path", BindingFlags.Public | BindingFlags.Instance);

            // Helper local function to register all controls from any device
            void RegisterDeviceControls(string deviceTypeName, string controlTypeName)
            {
                var deviceType = ReflectionUtility.GetTypeByName($"{deviceTypeName}, Unity.InputSystem");
                if (deviceType == null)
                {
                    ExplorerCore.LogWarning($"Device type not found: {deviceTypeName}");
                    return;
                }

                var currentProp = deviceType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                var deviceInstance = currentProp?.GetValue(null);
                if (deviceInstance == null)
                {
                    ExplorerCore.LogWarning($"{deviceTypeName}.current is null!");
                    return;
                }

                foreach (var prop in deviceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.GetIndexParameters().Length > 0)
                        continue;

                    if (prop.PropertyType.FullName == controlTypeName)
                    {
                        var control = prop.GetValue(deviceInstance);
                        if (control == null)
                            continue;

                        string path = pathProp?.GetValue(control) as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            buttonControls[path.ToLower()] = control;
                            //ExplorerCore.LogWarning($"Registered control: {path}");
                        }
                    }
                }
            }

            // Register keyboard keys
            RegisterDeviceControls("UnityEngine.InputSystem.Keyboard", "UnityEngine.InputSystem.Controls.KeyControl");
            // Register mouse buttons
            RegisterDeviceControls("UnityEngine.InputSystem.Mouse", "UnityEngine.InputSystem.Controls.ButtonControl");
            // Register gamepad buttons (buttonSouth, buttonNorth, etc.)
            RegisterDeviceControls("UnityEngine.InputSystem.Gamepad", "UnityEngine.InputSystem.Controls.ButtonControl");
        }

        // Dictionaries to store input states
        private static Dictionary<string, bool> buttonPressedStates = new Dictionary<string, bool>();
        private static Dictionary<string, bool> buttonWasPressedStates = new Dictionary<string, bool>();
        private static Dictionary<string, bool> actionInProgressStates = new Dictionary<string, bool>();
        private static Dictionary<string, bool> actionWasPerformedStates = new Dictionary<string, bool>();

        public static bool GetButtonPressed(string buttonName)
        {
            string normalizedName = buttonName.ToLower();
            normalizedName = PropToKeycode(normalizedName);
            //ExplorerCore.LogWarning($"Getting button pressed state for: {normalizedName}");
            if (buttonControls.TryGetValue(normalizedName, out var button))
            {
                isPressedProp.GetValue(button);
                return buttonPressedStates.TryGetValue(normalizedName, out bool value) && value;
            }
            return false;
        }

        public static bool GetButtonWasPressed(string buttonName)
        {
            string normalizedName = buttonName.ToLower();
            normalizedName = PropToKeycode(normalizedName);
            //ExplorerCore.LogWarning($"Getting button was pressed state for: {normalizedName}");
            if (buttonControls.TryGetValue(normalizedName, out var button))
            {
                wasPressedProp.GetValue(button);
                return buttonWasPressedStates.TryGetValue(normalizedName, out bool value) && value;
            }
            return false;
        }

        public static bool GetActionInProgress(string actionName)
        {
            string normalizedName = actionName.ToLower();
            normalizedName = PropToKeycode(normalizedName);
            //ExplorerCore.LogWarning($"Getting button action in progress state for: {normalizedName}");
            if (buttonControls.TryGetValue(normalizedName, out var button))
            {
                isInProgressMethod.Invoke(button, new object[] {});
                return actionInProgressStates.TryGetValue(normalizedName, out bool value) && value;
            }
            return false;
        }

        public static bool GetKey(KeyCode key)
        {
            string buttonName = key.ToString();
            return GetButtonPressed($"/Keyboard/{buttonName}");
        }

        public static bool GetMouseButton(int button)
        {
            var mouseType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            var currentProp = mouseType?.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            var mouseInstance = currentProp?.GetValue(null);
            if (mouseInstance == null) return false;

            // Define which mouse button corresponds to which property
            string propName = button switch
            {
                0 => "leftButton",
                1 => "rightButton",
                2 => "middleButton",
                3 => "forwardButton",
                4 => "backButton",
                _ => null
            };

            if (propName == null)
                return false;

            var buttonProp = mouseType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            var buttonControl = buttonProp?.GetValue(mouseInstance);
            if (buttonControl == null)
                return false;

            // Use reflection to check if it's pressed
            var buttonControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem");
            var isPressedProp = buttonControlType?.GetProperty("isPressed", BindingFlags.Public | BindingFlags.Instance);
            var result = isPressedProp?.GetValue(buttonControl);

            return buttonPressedStates.TryGetValue($"/mouse/{propName.ToLower()}", out bool value) && value;
        }

        public static void Postfix(object __instance, ref bool __result)
        {
            try
            {
                if (__instance != null)
                {
                    Type type = __instance.GetType();
                    string controlPath = string.Empty;

                    try
                    {
                        var pathProp = type.GetProperty("path") ?? type.GetProperty("name");
                        if (pathProp != null)
                        {
                            var value = pathProp.GetValue(__instance);
                            controlPath = value?.ToString() ?? string.Empty;
                        }
                    }
                    catch (Exception ex) 
                    { 
                        ExplorerCore.LogWarning($"Error getting control path: {ex.Message}");
                    }

                    // Store in appropriate dictionary based on calling method
                    var method = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod();
                    if (method != null)
                    {
                        string methodName = method.Name;

                        try
                        {
                            string normalizedPath = controlPath.ToLower();
                            if (methodName.Contains("IsPressed") || methodName.Contains("get_isPressed") || methodName.Contains("IsValueConsideredPressed"))
                            {
                                //if (__result) ExplorerCore.LogWarning($"Storing button pressed state for {normalizedPath}: {__result}");
                                buttonPressedStates[normalizedPath] = __result;
                            }
                            else if (methodName.Contains("WasPressedThisFrame") || methodName.Contains("get_wasPressedThisFrame"))
                            {
                                //if (__result) ExplorerCore.LogWarning($"Storing button was pressed this frame state for {normalizedPath}: {__result}");
                                buttonWasPressedStates[normalizedPath] = __result;
                            }
                            else if (methodName.Contains("IsInProgress"))
                            {
                                //if (__result) ExplorerCore.LogWarning($"Storing action in progress state for {normalizedPath}: {__result}");
                                actionInProgressStates[normalizedPath] = __result;
                            }
                            else if (methodName.Contains("WasPerformedThisFrame") || methodName.Contains("get_WasPerformedThisFrame"))
                            {
                                //if (__result) ExplorerCore.LogWarning($"Storing action was performed this frame state for {normalizedPath}: {__result}");
                                actionWasPerformedStates[normalizedPath] = __result;
                            }
                            else
                            {
                                ExplorerCore.LogWarning($"Method name did not match any known patterns: {methodName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            ExplorerCore.LogWarning($"Error storing input state: {ex.Message}");
                        }
                    }
                    else
                    {
                        ExplorerCore.LogWarning("Could not get calling method from stack trace");
                    }
                }

                if (FreeCamPanel.ShouldOverrideInput())
                {
                    __result = false;
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Error in OverrideNewInput: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static bool Prefix(object __instance, ref bool __runOriginal)
        {
            if (FreeCamPanel.ShouldOverrideInput())
            {
                return true;
            }

            return true;
        }

        // Input system property name doesnt match the KeyCode enum name, so we need to map some
        private static string PropToKeycode(string propName)
        {
            return propName switch
            {
                "/keyboard/leftcontrol" => "/keyboard/leftctrl",
                "/keyboard/rightcontrol" => "/keyboard/rightctrl",
                "/keyboard/keypad0" => "/keyboard/numpad0",
                "/keyboard/keypad1" => "/keyboard/numpad1",
                "/keyboard/keypad2" => "/keyboard/numpad2",
                "/keyboard/keypad3" => "/keyboard/numpad3",
                "/keyboard/keypad4" => "/keyboard/numpad4",
                "/keyboard/keypad5" => "/keyboard/numpad5",
                "/keyboard/keypad6" => "/keyboard/numpad6",
                "/keyboard/keypad7" => "/keyboard/numpad7",
                "/keyboard/keypad8" => "/keyboard/numpad8",
                "/keyboard/keypad9" => "/keyboard/numpad9",
                "/keyboard/keypaddivide" => "/keyboard/numpaddivide",
                "/keyboard/keypadmultiply" => "/keyboard/numpadmultiply",
                "/keyboard/keypadminus" => "/keyboard/numpadminus",
                "/keyboard/keypadplus" => "/keyboard/numpadplus",
                "/keyboard/keypadperiod" => "/keyboard/numpadperiod",
                "/keyboard/keypadequals" => "/keyboard/numpadequals",
                "/keyboard/keypadenter" => "/keyboard/numpadenter",
                "/keyboard/alpha0" => "/keyboard/0",
                "/keyboard/alpha1" => "/keyboard/1",
                "/keyboard/alpha2" => "/keyboard/2",
                "/keyboard/alpha3" => "/keyboard/3",
                "/keyboard/alpha4" => "/keyboard/4",
                "/keyboard/alpha5" => "/keyboard/5",
                "/keyboard/alpha6" => "/keyboard/6",
                "/keyboard/alpha7" => "/keyboard/7",
                "/keyboard/alpha8" => "/keyboard/8",
                "/keyboard/alpha9" => "/keyboard/9",
                "/keyboard/print" => "/keyboard/printscreen",
                _ => propName
            };
        }
    }
}
