using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib.Input;
#if UNHOLLOWER
using IL2CPPUtils = UnhollowerBaseLib.UnhollowerUtils;
#endif
#if INTEROP
using IL2CPPUtils = Il2CppInterop.Common.Il2CppInteropUtils;
#endif

namespace UnityExplorer
{
    
    public class IInputManager {

        private static Dictionary<KeyCode, bool> getKeyDict = new Dictionary<KeyCode, bool>();
        private static Dictionary<KeyCode, bool> getKeyDownDict = new Dictionary<KeyCode, bool>();
        private static Dictionary<KeyCode, bool> getKeyUpDict = new Dictionary<KeyCode, bool>();

        private static Dictionary<int, bool> getMouseButton = new Dictionary<int, bool>();
        private static Dictionary<int, bool> getMouseButtonDown = new Dictionary<int, bool>();

        public static Vector3 MousePosition => InputManager.MousePosition;

        public static void Setup(){
#if MONO
            Type inputClass = typeof(Input);
#endif
#if CPP
            Type inputClass = typeof(InputManager);
#endif
            try
            {
                MethodInfo getKeyTarget = inputClass.GetMethod("GetKey", new Type[] {typeof(string)});
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyTarget) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(getKeyTarget,
                    postfix: new(AccessTools.Method(typeof(IInputManager), nameof(OverrideKeyString))));
            }
            catch { }

            try
            {
                MethodInfo getKeyTarget = inputClass.GetMethod("GetKey", new Type[] {typeof(KeyCode)});
                //ExplorerCore.LogWarning(getKeyTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyTarget) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(getKeyTarget,
                    postfix: new(AccessTools.Method(typeof(IInputManager), nameof(OverrideKeyKeyCode))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyDownTarget = inputClass.GetMethod("GetKeyDown", new Type[] {typeof(string)});
                //ExplorerCore.LogWarning(getKeyDownTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyDownTarget) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(getKeyDownTarget,
                    postfix: new(AccessTools.Method(typeof(IInputManager), nameof(OverrideKeyDownString))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyDownTarget = inputClass.GetMethod("GetKeyDown", new Type[] {typeof(KeyCode)});
                //ExplorerCore.LogWarning(getKeyDownTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyDownTarget) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(getKeyDownTarget,
                    postfix: new(AccessTools.Method(typeof(IInputManager), nameof(OverrideKeyDownKeyCode))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyUpTarget = inputClass.GetMethod("GetKeyUp", new Type[] {typeof(string)});
                //ExplorerCore.LogWarning(getKeyUpTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyUpTarget) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(getKeyUpTarget,
                    postfix: new(AccessTools.Method(typeof(IInputManager), nameof(OverrideKeyUpString))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyUpTarget = inputClass.GetMethod("GetKeyUp", new Type[] {typeof(KeyCode)});
                //ExplorerCore.LogWarning(getKeyUpTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyUpTarget) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(getKeyUpTarget,
                    postfix: new(AccessTools.Method(typeof(IInputManager), nameof(OverrideKeyUpKeyCode))));
            }
            catch {  }

            try
            {
                MethodInfo getMouseButtonTarget = inputClass.GetMethod("GetMouseButton", new Type[] {typeof(int)});
                //ExplorerCore.LogWarning(getMouseButtonTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getMouseButtonTarget) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(getMouseButtonTarget,
                    postfix: new(AccessTools.Method(typeof(IInputManager), nameof(OverrideMouseButton))));
            }
            catch {  }

            try
            {
                MethodInfo getMouseButtonDownTarget = inputClass.GetMethod("GetMouseButtonDown", new Type[] {typeof(int)});
                //ExplorerCore.LogWarning(getMouseButtonDownTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getMouseButtonDownTarget) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(getMouseButtonDownTarget,
                    postfix: new(AccessTools.Method(typeof(IInputManager), nameof(OverrideMouseButtonDown))));
            }
            catch {  }
        }

        // Postfix functions

        public static void OverrideKeyString(ref bool __result, ref string key)
        {
            KeyCode thisKeyCode = (KeyCode) System.Enum.Parse(typeof(KeyCode), key);
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

        public static void OverrideKeyDownString(ref bool __result, ref string key)
        {
            KeyCode thisKeyCode = (KeyCode) System.Enum.Parse(typeof(KeyCode), key);
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

        public static void OverrideKeyUpString(ref bool __result, ref string key)
        {
            KeyCode thisKeyCode = (KeyCode) System.Enum.Parse(typeof(KeyCode), key);
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
            // Since UnityExplorer uses Unity's native UI for its menu, we can't switch off the mouse interaction with it on this wrapper.
            // Therefore, if we still want to interact with the Unity Explorer menu we would need to let the button action pass through when it's open.
            if (FreeCamPanel.ShouldOverrideInput() && !(button == 0 && UIManager.ShowMenu)){
                __result = false;
            }
        }

        public static void OverrideMouseButtonDown(ref bool __result, ref int button)
        {
            getMouseButtonDown[button] = __result;
            // Since UnityExplorer uses Unity's native UI for its menu, we can't switch off the mouse interaction with it on this wrapper.
            // Therefore, if we still want to interact with the Unity Explorer menu we would need to let the button action pass through when it's open.
            if (FreeCamPanel.ShouldOverrideInput() && button != 0){
                __result = false;
            }
        }

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
    }
}
