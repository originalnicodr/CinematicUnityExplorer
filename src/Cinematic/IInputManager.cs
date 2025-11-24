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
                    string buttonName = $"/keyboard/{key.ToString()}".ToLower();
                    return INewInputSystem.GetButtonWasPressed(buttonName);
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
                    string buttonName = $"/keyboard/{key.ToString()}".ToLower();
                    return INewInputSystem.GetButtonWasReleased(buttonName);
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
                    return INewInputSystem.GetMouseButtonDown(button);
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
        // --- Reflected members ---
        private static PropertyInfo isPressedProp;
        private static PropertyInfo wasPressedProp;
        private static PropertyInfo wasReleasedProp;
        private static MethodInfo isValueConsideredPressed;
        private static MethodInfo isPressedMethod;
        private static MethodInfo isInProgressMethod;
        private static MethodInfo wasPressedMethod;
        private static MethodInfo wasPerformedMethod;

        // --- State dictionaries ---
        internal static readonly Dictionary<string, object> buttonControls = new();
        internal static readonly Dictionary<string, object> gamepadButtonControls = new();
        internal static readonly Dictionary<string, bool> buttonPressedStates = new();
        internal static readonly Dictionary<string, bool> buttonWasPressedStates = new();
        internal static readonly Dictionary<string, bool> buttonWasReleasedStates = new();
        private static readonly Dictionary<string, bool> buttonIsValueConsideredPressedStates = new();
        private static readonly Dictionary<string, bool> actionInProgressStates = new();
        private static readonly Dictionary<string, bool> actionWasPerformedStates = new();

        // --- Initialization ---
        public static void Init()
        {
            Type buttonControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem");
            Type inputActionType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputAction, Unity.InputSystem");

            if (buttonControlType == null || inputActionType == null)
            {
                ExplorerCore.LogWarning("Unity.InputSystem types not found; InputSystem integration disabled.");
                return;
            }

            // === Patch all relevant ButtonControl properties ===
            PatchButtonControl(buttonControlType);

            // === Patch InputAction methods ===
            PatchInputAction(inputActionType);

            // === Cache controls ===
            GetButtonControls();

            IGamepadInputInterceptor.Init();
        }

        private static void PatchButtonControl(Type buttonControlType)
        {
            isPressedProp = buttonControlType.GetProperty("isPressed");
            wasPressedProp = buttonControlType.GetProperty("wasPressedThisFrame");
            wasReleasedProp = buttonControlType.GetProperty("wasReleasedThisFrame");
            isValueConsideredPressed = buttonControlType.GetMethod("IsValueConsideredPressed");

            PatchPropertyGetter(isPressedProp, nameof(Postfix_IsPressed));
            PatchPropertyGetter(wasPressedProp, nameof(Postfix_WasPressedThisFrame));
            PatchPropertyGetter(wasReleasedProp, nameof(Postfix_WasReleasedThisFrame));
            PatchPropertyGetter(wasReleasedProp, nameof(Postfix_IsValueConsideredPressed));
        }

        private static void PatchInputAction(Type inputActionType)
        {
            isPressedMethod = inputActionType.GetMethod("IsPressed");
            isInProgressMethod = inputActionType.GetMethod("IsInProgress");
            wasPressedMethod = inputActionType.GetMethod("WasPressedThisFrame");
            wasPerformedMethod = inputActionType.GetMethod("WasPerformedThisFrame");

            PatchMethod(isPressedMethod, nameof(Postfix_IsPressed));
            PatchMethod(isInProgressMethod, nameof(Postfix_IsInProgress));
            PatchMethod(wasPressedMethod, nameof(Postfix_WasPressedThisFrame));
            PatchMethod(wasPerformedMethod, nameof(Postfix_WasPerformedThisFrame));
        }

        private static void PatchPropertyGetter(PropertyInfo prop, string postfixName)
        {
            if (prop?.GetGetMethod() == null) return;
#if CPP
            if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(prop.GetGetMethod()) == null)
                throw new Exception();
#endif
            ExplorerCore.Harmony.Patch(prop.GetGetMethod(),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), postfixName)));
        }

        private static void PatchMethod(MethodInfo method, string postfixName)
        {
            if (method == null) return;
#if CPP
            if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method) == null)
                throw new Exception();
#endif
            ExplorerCore.Harmony.Patch(method,
                postfix: new HarmonyMethod(AccessTools.Method(typeof(INewInputSystem), postfixName)));
        }

        // --- Cache button controls ---
        public static void GetButtonControls()
        {
            Type buttonControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem");
            PropertyInfo pathProp = buttonControlType?.GetProperty("path", BindingFlags.Public | BindingFlags.Instance);

            RegisterDeviceControls("UnityEngine.InputSystem.Keyboard", "UnityEngine.InputSystem.Controls.KeyControl", pathProp);
            RegisterDeviceControls("UnityEngine.InputSystem.Mouse", "UnityEngine.InputSystem.Controls.ButtonControl", pathProp);
            // Note: Gamepad buttons are registered by IGamepadInputInterceptor with proper path normalization
        }

        internal static void RegisterDeviceControls(string deviceTypeName, string controlTypeName, PropertyInfo pathProp)
        {
            Type deviceType = ReflectionUtility.GetTypeByName($"{deviceTypeName}, Unity.InputSystem");
            if (deviceType == null) return;

            PropertyInfo currentProp = deviceType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            object deviceInstance = currentProp?.GetValue(null, null);
            if (deviceInstance == null) return;

            foreach (PropertyInfo prop in deviceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                if (prop.PropertyType.FullName == controlTypeName)
                {
                    object control = prop.GetValue(deviceInstance, null);
                    if (control == null)
                        continue;

                    string path = pathProp?.GetValue(control, null) as string;
                    if (!string.IsNullOrEmpty(path))
                        buttonControls[path.ToLower()] = control;
                }
            }
        }

        // --- Access methods ---

        public static bool GetKey(KeyCode key)
        {
            string buttonName = PropToKeycode($"/keyboard/{key.ToString()}".ToLower());
            return GetButtonPressed(buttonName);
        }

        public static bool GetButtonPressed(string buttonName)
        {
            string key = buttonName.ToLower();
            if (buttonControls.TryGetValue(key, out object button))
            {
                isPressedProp?.GetValue(button, null);
                return buttonPressedStates.TryGetValue(key, out bool value) && value;
            }
            return false;
        }

        public static bool GetButtonWasPressed(string buttonName)
        {
            string key = buttonName.ToLower();
            if (buttonControls.TryGetValue(key, out object button))
            {
                wasPressedProp?.GetValue(button, null);
                return buttonWasPressedStates.TryGetValue(key, out bool value) && value;
            }
            return false;
        }

        public static bool GetButtonWasReleased(string buttonName)
        {
            string key = buttonName.ToLower();
            if (buttonControls.TryGetValue(key, out object button))
            {
                wasReleasedProp?.GetValue(button, null);
                return buttonWasReleasedStates.TryGetValue(key, out bool value) && value;
            }
            return false;
        }

        public static bool GetMouseButton(int button)
        {
            string buttonPath = button switch
            {
                0 => "/mouse/leftbutton",
                1 => "/mouse/rightbutton",
                2 => "/mouse/middlebutton",
                3 => "/mouse/forwardbutton",
                4 => "/mouse/backbutton",
                _ => $"/mouse/button{button}"
            };

            return GetButtonPressed(buttonPath);
        }

        public static bool GetMouseButtonDown(int button){
            string buttonPath = button switch
            {
                0 => "/mouse/leftbutton",
                1 => "/mouse/rightbutton",
                2 => "/mouse/middlebutton",
                3 => "/mouse/forwardbutton",
                4 => "/mouse/backbutton",
                _ => $"/mouse/button{button}"
            };
            return GetButtonWasPressed(buttonPath);
        }

        // --- Postfixes ---

        private static void Postfix_IsPressed(object __instance, ref bool __result)
            => StoreState(__instance, ref __result, buttonPressedStates);

        private static void Postfix_WasPressedThisFrame(object __instance, ref bool __result)
            => StoreState(__instance, ref __result, buttonWasPressedStates);

        private static void Postfix_WasReleasedThisFrame(object __instance, ref bool __result)
            => StoreState(__instance, ref __result, buttonWasReleasedStates);

        private static void Postfix_IsValueConsideredPressed(object __instance, ref bool __result)
            => StoreState(__instance, ref __result, buttonIsValueConsideredPressedStates);

        private static void Postfix_IsInProgress(object __instance, ref bool __result)
            => StoreState(__instance, ref __result, actionInProgressStates);

        private static void Postfix_WasPerformedThisFrame(object __instance, ref bool __result)
            => StoreState(__instance, ref __result, actionWasPerformedStates);

        private static void StoreState(object __instance, ref bool __result, Dictionary<string, bool> dict)
        {
            try
            {
                Type type = __instance.GetType();
                PropertyInfo pathProp = type.GetProperty("path") ?? type.GetProperty("name");
                string key = (pathProp?.GetValue(__instance, null)?.ToString() ?? string.Empty).ToLower();
                
                // Normalize gamepad paths so they match what IGamepadInputInterceptor registered
                key = IGamepadInputInterceptor.NormalizeControlPath(key);
                
                dict[key] = __result;

                if (FreeCamPanel.ShouldOverrideInput())
                {
                    __result = false;
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Failed to store state: {ex.Message}");
            }
        }

        // Input system property name doesnt match the KeyCode enum name, so we need to map some
        public static string PropToKeycode(string propName)
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
