// Uncomment to enable verbose logging in gamepad interceptor
//#define IGAMEPAD_DEBUG

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityExplorer;
using UnityExplorer.UI.Panels;

#if UNHOLLOWER
using IL2CPPUtils = UnhollowerBaseLib.UnhollowerUtils;
#endif
#if INTEROP
using IL2CPPUtils = Il2CppInterop.Common.Il2CppInteropUtils;
#endif

namespace UniverseLib.Input
{
    /// <summary>
    /// Reflection wrapper to interact with gamepad inputs while blocking their actual state from the game.
    /// </summary>
    public static class IGamepadInputInterceptor
    {
        // --- Configuration ---
        private static bool _isEnabled = false;
        private static int _targetGamepadIndex = 0;

        // --- Reflected types ---
        private static Type _gamepadType;
        private static Type _buttonControlType;
        private static Type _axisControlType;
        private static Type _vector2ControlType;
        private static Type _inputControlType;
        private static PropertyInfo _basePathProp;

        // --- Reflected properties and methods for triggering state capture ---
        private static PropertyInfo _buttonIsPressedProp;
        private static PropertyInfo _buttonWasPressedProp;
        private static PropertyInfo _buttonWasReleasedProp;
        private static MethodInfo _axisReadValueMethod;
        private static MethodInfo _vector2ReadValueMethod;

        // --- Cached reflection for device checking ---
        private static PropertyInfo _deviceProp;

        // --- Cached reflection for Vector2 property access ---
        private static PropertyInfo _vector2XProp;
        private static PropertyInfo _vector2YProp;

        // --- Cached reflection for gamepad control access ---
        private static PropertyInfo _gamepadAllProp;
        private static PropertyInfo _gamepadCurrentProp;
        private static PropertyInfo _gamepadLeftStickProp;
        private static PropertyInfo _gamepadRightStickProp;

        // --- Vendor name normalization ---
        private static readonly Dictionary<string, string> _vendorPrefixMap = new()
        {
            { "/xinputcontrollerwindows", "/gamepad" },
            { "/xinputcontroller", "/gamepad" },
            { "/dualshockgamepad", "/gamepad" },
            { "/dualsensegamepad", "/gamepad" },
            { "/dualshock4gamepadhid", "/gamepad" },
            { "/dualshock3gamepadhid", "/gamepad" },
            { "/androidgamepad", "/gamepad" },
            { "/switchprocontrollerhid", "/gamepad" },
        };

        // --- Control collections ---
        // Maps normalized paths to control objects
        private static readonly Dictionary<string, object> _axisControls = new();
        private static readonly Dictionary<string, object> _vector2Controls = new();

        // --- State dictionaries (populated by postfix patches) ---
        private static readonly Dictionary<string, float> _axisValues = new();
        private static readonly Dictionary<string, Vector2> _vector2Values = new();

        // --- Initialization ---

        /// <summary>
        /// Initializes the gamepad input interceptor. Must be called during game startup.
        /// </summary>
        public static void Init()
        {
            try
            {
#if IGAMEPAD_DEBUG
                ExplorerCore.LogWarning("[IGamepadInputInterceptor] === Initializing ===");
#endif
                
                LoadReflectedTypes();
                if (_gamepadType == null || _axisControlType == null)
                {
#if IGAMEPAD_DEBUG
                    ExplorerCore.LogWarning("[IGamepadInputInterceptor] Failed to load required types");
#endif
                    return;
                }

                // Only patch analog controls (buttons are handled by INewInputSystem)
                PatchAnalogControls();
                //INewInputSystem.Init() should have been run beforehand
                DiscoverGamepadControls();
                _isEnabled = true;

#if IGAMEPAD_DEBUG
                ExplorerCore.LogWarning("[IGamepadInputInterceptor] === Initialization complete ===");
#endif
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Init failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Enables input interception. When enabled, captured inputs will be replaced with default values.
        /// </summary>
        public static void Enable()
        {
            _isEnabled = true;
#if IGAMEPAD_DEBUG
            ExplorerCore.LogWarning("[IGamepadInputInterceptor] Input interception enabled");
#endif
        }

        /// <summary>
        /// Disables input interception. Game will receive normal gamepad input.
        /// </summary>
        public static void Disable()
        {
            _isEnabled = false;
#if IGAMEPAD_DEBUG
            ExplorerCore.LogWarning("[IGamepadInputInterceptor] Input interception disabled");
#endif
        }

        /// <summary>
        /// Sets which gamepad to target when multiple gamepads are connected.
        /// </summary>
        public static void SetTargetGamepad(int index)
        {
            _targetGamepadIndex = index;
#if IGAMEPAD_DEBUG
            ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Target gamepad set to index {index}");
#endif
            // Re-discover controls for the new gamepad
            ClearControlCaches();
            DiscoverGamepadControls();
        }

        // --- Type Loading ---

        private static void LoadReflectedTypes()
        {
            _gamepadType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Gamepad, Unity.InputSystem");
            _buttonControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem");
            _axisControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.AxisControl, Unity.InputSystem");
            _vector2ControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.Vector2Control, Unity.InputSystem");
            _inputControlType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputControl, Unity.InputSystem");

            _basePathProp = _inputControlType?.GetProperty("path", BindingFlags.Public | BindingFlags.Instance);

            _buttonIsPressedProp = _buttonControlType?.GetProperty("isPressed", BindingFlags.Public | BindingFlags.Instance);
            _buttonWasPressedProp = _buttonControlType?.GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance);
            _buttonWasReleasedProp = _buttonControlType?.GetProperty("wasReleasedThisFrame", BindingFlags.Public | BindingFlags.Instance);

            _axisReadValueMethod = _axisControlType?.GetMethod("ReadValue", Type.EmptyTypes);
            _vector2ReadValueMethod = _vector2ControlType?.GetMethod("ReadValue", Type.EmptyTypes);

            _deviceProp = _inputControlType?.GetProperty("device", BindingFlags.Public | BindingFlags.Instance);

            _vector2XProp = _vector2ControlType?.GetProperty("x", BindingFlags.Public | BindingFlags.Instance);
            _vector2YProp = _vector2ControlType?.GetProperty("y", BindingFlags.Public | BindingFlags.Instance);

            _gamepadAllProp = _gamepadType?.GetProperty("all", BindingFlags.Static | BindingFlags.Public);
            _gamepadCurrentProp = _gamepadType?.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
            _gamepadLeftStickProp = _gamepadType?.GetProperty("leftStick", BindingFlags.Public | BindingFlags.Instance);
            _gamepadRightStickProp = _gamepadType?.GetProperty("rightStick", BindingFlags.Public | BindingFlags.Instance);

#if IGAMEPAD_DEBUG
            LogLoadedType("Gamepad", _gamepadType);
            LogLoadedType("ButtonControl", _buttonControlType);
            LogLoadedType("AxisControl", _axisControlType);
            LogLoadedType("Vector2Control", _vector2ControlType);
#endif
        }

        private static void LogLoadedType(string name, Type type)
        {
            if (type != null)
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Loaded type: {name}");
            else
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Failed to load type: {name}");
        }

        // --- Patching ---

        private static void PatchAnalogControls()
        {
            try
            {
                if (_vector2ControlType != null)
                {
                    MethodInfo vector2ReadValueMethod = _vector2ControlType.GetMethod("ReadValue", 
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, 
                        null, Type.EmptyTypes, null);
                    
                    // If not found with DeclaredOnly, try getting from base type
                    if (vector2ReadValueMethod == null)
                    {
                        Type baseType = _vector2ControlType.BaseType;
                        if (baseType != null && baseType.IsGenericType)
                        {
                            vector2ReadValueMethod = baseType.GetMethod("ReadValue", 
                                BindingFlags.Public | BindingFlags.Instance, 
                                null, Type.EmptyTypes, null);
                        }
                    }
                    
                    if (vector2ReadValueMethod != null)
                    {
                        PatchMethod(vector2ReadValueMethod, null, nameof(Postfix_Vector2ReadValue));
#if IGAMEPAD_DEBUG
                        ExplorerCore.LogWarning("[IGamepadInputInterceptor] Patched Vector2Control.ReadValue()");
#endif
                    }
                }

                if (_axisControlType != null)
                {
                    MethodInfo readValueMethod = _axisControlType.GetMethod("ReadValue", 
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, 
                        null, Type.EmptyTypes, null);
                    
                    // If not found with DeclaredOnly, try getting from base type
                    if (readValueMethod == null)
                    {
                        Type baseType = _axisControlType.BaseType;
                        if (baseType != null && baseType.IsGenericType)
                        {
                            readValueMethod = baseType.GetMethod("ReadValue", 
                                BindingFlags.Public | BindingFlags.Instance, 
                                null, Type.EmptyTypes, null);
                        }
                    }
                    
                    if (readValueMethod != null)
                    {
                        PatchMethod(readValueMethod, null, nameof(Postfix_AxisReadValue));
#if IGAMEPAD_DEBUG
                        ExplorerCore.LogWarning("[IGamepadInputInterceptor] Patched AxisControl.ReadValue()");
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Error during patching: {ex.Message}");
            }
        }

        private static void PatchMethod(MethodInfo method, string prefixName, string postfixName)
        {
            try
            {
                if (method == null)
                    return;

#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method) == null)
                    return;
#endif

                MethodInfo prefixMethod = prefixName != null ? AccessTools.Method(typeof(IGamepadInputInterceptor), prefixName) : null;
                MethodInfo postfixMethod = postfixName != null ? AccessTools.Method(typeof(IGamepadInputInterceptor), postfixName) : null;

                ExplorerCore.Harmony.Patch(method,
                    prefix: prefixMethod != null ? new HarmonyMethod(prefixMethod) : null,
                    postfix: postfixMethod != null ? new HarmonyMethod(postfixMethod) : null);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Failed to patch method: {ex.Message}");
            }
        }

        // --- Control Discovery ---

        private static void DiscoverGamepadControls()
        {
            try
            {
#if IGAMEPAD_DEBUG
                ExplorerCore.LogWarning("[IGamepadInputInterceptor] Discovering gamepad controls...");
#endif

                object gamepadInstance = GetGamepadInstance(_targetGamepadIndex);
                if (gamepadInstance == null)
                {
#if IGAMEPAD_DEBUG
                    ExplorerCore.LogWarning("[IGamepadInputInterceptor] No gamepad found at specified index");
#endif
                    return;
                }

                // Instead of iterating allControls and trying to cast them,
                // directly access the typed control properties on the Gamepad class.
                // This gives us the controls with their correct types immediately.

                // List of known public control properties on Gamepad
                string[] buttonProperties = new[]
                {
                    "buttonSouth", "buttonNorth", "buttonWest", "buttonEast",
                    "leftStickButton", "rightStickButton", "leftShoulder", "rightShoulder",
                    "startButton", "selectButton", "leftTrigger", "rightTrigger"
                };

                string[] vector2Properties = new[] { "leftStick", "rightStick" };

                // Clear any existing gamepad buttons from INewInputSystem before registering new ones
                // This prevents duplicates when switching gamepads or re-initializing
                //var keysToRemove = INewInputSystem.buttonControls.Keys.Where(k => k.StartsWith("/gamepad") || k.Contains("gamepad")).ToList();
                //foreach (var key in keysToRemove)
                //{
                //    INewInputSystem.buttonControls.Remove(key);
                //}

                // Register all button controls
                foreach (string propName in buttonProperties)
                {
                    try
                    {
                        PropertyInfo prop = _gamepadType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                        if (prop != null)
                        {
                            object control = prop.GetValue(gamepadInstance, BindingFlags.Default, null, null, null);
                            if (control != null)
                            {
                                string normalizedPath = ExtractAndNormalizePath(control);
                                if (!string.IsNullOrEmpty(normalizedPath))
                                {
                                    INewInputSystem.gamepadButtonControls[normalizedPath] = control;
#if IGAMEPAD_DEBUG
                                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Registered button: {normalizedPath} ({propName})");
#endif
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Failed to access button {propName}: {ex.Message}");
                    }
                }

                // Register D-pad individual direction buttons
                try
                {
                    PropertyInfo dpadProp = _gamepadType.GetProperty("dpad", BindingFlags.Public | BindingFlags.Instance);
                    if (dpadProp != null)
                    {
                        object dpadControl = dpadProp.GetValue(gamepadInstance, BindingFlags.Default, null, null, null);
                        if (dpadControl != null)
                        {
                            string[] dpadDirections = new[] { "up", "down", "left", "right" };
                            foreach (string direction in dpadDirections)
                            {
                                try
                                {
                                    PropertyInfo dpadDirProp = dpadControl.GetType().GetProperty(direction, BindingFlags.Public | BindingFlags.Instance);
                                    if (dpadDirProp != null)
                                    {
                                        object buttonControl = dpadDirProp.GetValue(dpadControl, BindingFlags.Default, null, null, null);
                                        if (buttonControl != null)
                                        {
                                            string buttonPath = ExtractAndNormalizePath(buttonControl);
                                            if (!string.IsNullOrEmpty(buttonPath))
                                            {
                                                INewInputSystem.gamepadButtonControls[buttonPath] = buttonControl;
#if IGAMEPAD_DEBUG
                                                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Registered dpad button: {buttonPath} (dpad.{direction})");
#endif
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Failed to access dpad.{direction}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Failed to access dpad: {ex.Message}");
                }

                // Register all vector2 controls (sticks)
                foreach (string propName in vector2Properties)
                {
                    try
                    {
                        PropertyInfo prop = _gamepadType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                        if (prop != null)
                        {
                            object control = prop.GetValue(gamepadInstance, BindingFlags.Default, null, null, null);
                            if (control != null)
                            {
                                string normalizedPath = ExtractAndNormalizePath(control);
                                if (!string.IsNullOrEmpty(normalizedPath))
                                {
                                    _vector2Controls[normalizedPath] = control;
#if IGAMEPAD_DEBUG
                                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Registered vector2: {normalizedPath} ({propName})");
#endif
                                }

                                // Also register individual stick axes as axis controls
                                // Use the stick name to differentiate between left and right sticks
                                string[] axisNames = new[] { "x", "y" };
                                foreach (string axisName in axisNames)
                                {
                                    try
                                    {
                                        PropertyInfo axisProp = control.GetType().GetProperty(axisName, BindingFlags.Public | BindingFlags.Instance);
                                        if (axisProp != null)
                                        {
                                            object axisControl = axisProp.GetValue(control, BindingFlags.Default, null, null, null);
                                            if (axisControl != null)
                                            {
                                                string rawAxisPath = ExtractAndNormalizePath(axisControl);
                                                if (!string.IsNullOrEmpty(rawAxisPath))
                                                {
                                                    // Inject the stick name into the path: /gamepad/x -> /gamepad/leftstick/x
                                                    string stickName = propName.ToLowerInvariant(); // "leftstick" or "rightstick"
                                                    string axisPath = $"/gamepad/{stickName}/{axisName}";
                                                    _axisControls[axisPath] = axisControl;
#if IGAMEPAD_DEBUG
                                                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Registered stick axis: {axisPath} ({propName}.{axisName})");
#endif
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Failed to access {propName}.{axisName}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Failed to access vector2 {propName}: {ex.Message}");
                    }
                }

                // Register triggers as AXIS controls so analog values can be captured
                // Triggers are ButtonControl but also support analog values
                string[] triggerProperties = new[] { "leftTrigger", "rightTrigger" };
                foreach (string propName in triggerProperties)
                {
                    try
                    {
                        PropertyInfo prop = _gamepadType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                        if (prop != null)
                        {
                            object control = prop.GetValue(gamepadInstance, BindingFlags.Default, null, null, null);
                            if (control != null)
                            {
                                string normalizedPath = ExtractAndNormalizePath(control);
                                if (!string.IsNullOrEmpty(normalizedPath))
                                {
                                    // Add trigger axis path (e.g., /gamepad/lefttrigger for analog value)
                                    _axisControls[normalizedPath] = control;
#if IGAMEPAD_DEBUG
                                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Registered trigger axis: {normalizedPath} ({propName})");
#endif
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Failed to access {propName}: {ex.Message}");
                    }
                }

                int totalControls = INewInputSystem.gamepadButtonControls.Count + _axisControls.Count + _vector2Controls.Count;
#if IGAMEPAD_DEBUG
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Registered {totalControls} controls " +
                    $"({INewInputSystem.gamepadButtonControls.Count} buttons, {_axisControls.Count} axes, {_vector2Controls.Count} vectors)");
#endif
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Error discovering controls: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static object GetGamepadInstance(int index)
        {
            try
            {
                // Try to manually select the controller by index from the all collection
                if (_gamepadAllProp != null)
                {
                    object allGamepads = _gamepadAllProp.GetValue(null, BindingFlags.Default, null, null, null);
                    if (allGamepads != null)
                    {
                        Type collectionType = allGamepads.GetType();
                        PropertyInfo countProp = collectionType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                        PropertyInfo itemProp = collectionType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);

                        if (countProp != null && itemProp != null)
                        {
                            int count = (int)countProp.GetValue(allGamepads, BindingFlags.Default, null, null, null);
                            if (index >= 0 && index < count)
                            {
                                return itemProp.GetValue(allGamepads, new object[] { index });
                            }
                        }
                    }
                }

                // Fallback to Gamepad.current
                if (_gamepadCurrentProp != null)
                {
                    return _gamepadCurrentProp.GetValue(null, BindingFlags.Default, null, null, null);
                }

                return null;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Error getting gamepad instance: {ex.Message}");
                return null;
            }
        }

        // Some builds have problems when trying to retrieve property names, so we have to get some fallbacks into place while also trying to get a unified format.
        private static string ExtractAndNormalizePath(object control)
        {
            if (control == null)
                return null;

            string path = null;

            // Method 1: Try ToString() - safest on IL2CPP
            try
            {
                string toString = control.ToString();
                if (!string.IsNullOrEmpty(toString))
                {
                    // Parse path from ToString (e.g., "ButtonControl{/xinputcontrollerwindows/buttonSouth}")
                    int slashIndex = toString.IndexOf('/');
                    if (slashIndex >= 0)
                    {
                        int closeIndex = toString.IndexOf('}', slashIndex);
                        if (closeIndex >= 0)
                        {
                            path = toString.Substring(slashIndex, closeIndex - slashIndex);
                        }
                    }
                }
            }
            catch { }

            // Method 2: Try reflection on path property (managed builds)
#if CPP
            if (string.IsNullOrEmpty(path))
            {
                try
                {
                    if (_basePathProp != null)
                    {
                        object pathObj = _basePathProp.GetValue(control, BindingFlags.Default, null, null, null);
                        path = pathObj as string;
                    }
                }
                catch { }
            }
#endif

            // Method 3: Try via name property (fallback)
            if (string.IsNullOrEmpty(path))
            {
                try
                {
                    PropertyInfo nameProp = control.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance);
                    if (nameProp != null)
                    {
                        string controlName = nameProp.GetValue(control, BindingFlags.Default, null, null, null) as string;
                        if (!string.IsNullOrEmpty(controlName))
                        {
                            path = $"/gamepad/{controlName}";
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(path))
                return null;

            return NormalizeControlPath(path);
        }

        /// <summary>
        /// Normalizes gamepad control paths to a common format across different controller types.
        /// </summary>
        public static string NormalizeControlPath(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath))
                return rawPath;

            string path = rawPath.ToLowerInvariant().Trim();

            foreach (var kvp in _vendorPrefixMap)
            {
                if (path.StartsWith(kvp.Key))
                {
                    path = kvp.Value + path.Substring(kvp.Key.Length);
                    break;
                }
            }

            // Remove numeric gamepad suffix (/gamepad1 → /gamepad)
            const string prefix = "/gamepad";
            int prefixLen = prefix.Length;
            if (path.StartsWith(prefix) &&
                path.Length > prefixLen &&
                char.IsDigit(path[prefixLen]))
            {
                path = prefix + path.Substring(prefixLen + 1);
            }

            return path;
        }

        private static void ClearControlCaches()
        {
            INewInputSystem.gamepadButtonControls.Clear();
            _axisControls.Clear();
            _vector2Controls.Clear();
            _axisValues.Clear();
            _vector2Values.Clear();
        }

        // --- Public API ---

        /// <summary>
        /// Gets whether a button is currently pressed.
        /// </summary>
        /// <param name="buttonPath">Normalized path like "/gamepad/buttonsouth" or vendor-specific path</param>
        public static bool IsButtonPressed(string buttonPath)
        {
            if (!_isEnabled)
                return false;

            string key = buttonPath.ToLower();

#if IGAMEPAD_DEBUG
            ExplorerCore.LogWarning($"[IGamepadInputInterceptor] IsButtonPressed querying: {key}");
#endif
            
            // Trigger the property getter to populate state via INewInputSystem
            if (INewInputSystem.gamepadButtonControls.TryGetValue(key, out object button) && _buttonIsPressedProp != null)
            {
#if IGAMEPAD_DEBUG
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] IsButtonPressed found button, invoking getter");
#endif
                _buttonIsPressedProp.GetValue(button, BindingFlags.Default, null, null, null);
            }
#if IGAMEPAD_DEBUG
            else
            {
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] IsButtonPressed NOT found in gamepadButtonControls. Available: {string.Join(", ", INewInputSystem.gamepadButtonControls.Keys)}");
            }
#endif
            bool result = INewInputSystem.buttonPressedStates.TryGetValue(key, out bool pressed) && pressed;
#if IGAMEPAD_DEBUG
            //ExplorerCore.LogWarning($"[IGamepadInputInterceptor] IsButtonPressed returning: {result}");
#endif
            return result;
        }

        /// <summary>
        /// Gets whether a button was pressed this frame.
        /// </summary>
        public static bool WasButtonPressedThisFrame(string buttonPath)
        {
            if (!_isEnabled)
                return false;

            string key = buttonPath.ToLower();
            
            // Trigger the property getter to populate state via INewInputSystem
            if (INewInputSystem.gamepadButtonControls.TryGetValue(key, out object button) && _buttonWasPressedProp != null)
            {
                _buttonWasPressedProp.GetValue(button, BindingFlags.Default, null, null, null);
            }

            return INewInputSystem.buttonWasPressedStates.TryGetValue(key, out bool pressed) && pressed;
        }

        /// <summary>
        /// Gets whether a button was released this frame.
        /// </summary>
        public static bool WasButtonReleasedThisFrame(string buttonPath)
        {
            if (!_isEnabled)
                return false;

            string key = buttonPath.ToLower();
            
            // Trigger the property getter to populate state via INewInputSystem
            if (INewInputSystem.gamepadButtonControls.TryGetValue(key, out object button) && _buttonWasReleasedProp != null)
            {
                _buttonWasReleasedProp.GetValue(button, BindingFlags.Default, null, null, null);
            }

            return INewInputSystem.buttonWasReleasedStates.TryGetValue(key, out bool released) && released;
        }

        /// <summary>
        /// Gets the current value of an analog axis (trigger, stick axis, etc).
        /// </summary>
        /// <param name="axisPath">Path like "/gamepad/lefttrigger" or "/gamepad/leftstick/x"</param>
        public static float GetAxisValue(string axisPath)
        {
            if (!_isEnabled)
                return 0f;

            string normalized = NormalizeControlPath(axisPath ?? "");
#if IGAMEPAD_DEBUG
            ExplorerCore.LogWarning($"[IGamepadInputInterceptor] GetAxisValue querying: {normalized}");
#endif
            
            // Trigger the ReadValue() method to populate state
            if (_axisControls.TryGetValue(normalized, out object axis) && _axisReadValueMethod != null)
            {
#if IGAMEPAD_DEBUG
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] GetAxisValue found axis, invoking ReadValue()");
#endif
                _axisReadValueMethod.Invoke(axis, null);
            }
#if IGAMEPAD_DEBUG
            else
            {
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] GetAxisValue NOT found for: {normalized}. Available: {string.Join(", ", _axisControls.Keys)}");
            }
#endif
            return _axisValues.TryGetValue(normalized, out float value) ? value : 0f;
        }

        /// <summary>
        /// Gets the current value of a 2D input (stick, dpad).
        /// </summary>
        /// <param name="vectorPath">Path like "/gamepad/leftstick" or "/gamepad/dpad"</param>
        public static Vector2 GetVector2Value(string vectorPath)
        {
            if (!_isEnabled)
                return new Vector2(0f, 0f);

            string normalized = NormalizeControlPath(vectorPath ?? "");
            
            // Trigger the ReadValue() method to populate state
            if (_vector2Controls.TryGetValue(normalized, out object vector2) && _vector2ReadValueMethod != null)
            {
                _vector2ReadValueMethod.Invoke(vector2, null);
            }

            return _vector2Values.TryGetValue(normalized, out var value) ? value : new Vector2(0f, 0f);
        }

        /// <summary>
        /// Gets all currently registered control paths.
        /// </summary>
        public static IEnumerable<string> GetRegisteredButtonPaths() => INewInputSystem.gamepadButtonControls.Keys;

        /// <summary>
        /// Gets all currently registered axis paths.
        /// </summary>
        public static IEnumerable<string> GetRegisteredAxisPaths() => _axisControls.Keys;

        /// <summary>
        /// Gets all currently registered vector2 paths.
        /// </summary>
        public static IEnumerable<string> GetRegisteredVector2Paths() => _vector2Controls.Keys;

        // --- Postfix Methods (capture state and optionally block) ---

        private static void Postfix_Vector2ReadValue(object __instance, ref object __result)
        {
#if IGAMEPAD_DEBUG
            ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Postfix_Vector2ReadValue called, instance={(__instance != null ? "not null" : "null")}, result={(__result != null ? "not null" : "null")}");
#endif
            if (__instance == null || __result == null)
                return;

            // Verify the vector2 control is properly attached to a device before proceeding
            try
            {
                if (_deviceProp != null)
                {
                    object device = _deviceProp.GetValue(__instance, BindingFlags.Default, null, null, null);
                    if (device == null)
                        return; // Control not attached to a device, skip
                }
            }
            catch
            {
                return; // If we can't check device, skip to be safe
            }

            string path = ExtractAndNormalizePath(__instance);
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    if (_vector2XProp != null && _vector2YProp != null)
                    {
                        float x = (float)_vector2XProp.GetValue(__result, BindingFlags.Default, null, null, null);
                        float y = (float)_vector2YProp.GetValue(__result, BindingFlags.Default, null, null, null);

#if IGAMEPAD_DEBUG
                        ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Postfix_Vector2ReadValue: {path} = ({x}, {y})");
#endif
                        _vector2Values[path] = new Vector2(x, y);

                        if (_isEnabled && FreeCamPanel.ShouldOverrideInput() && _vector2XProp.CanWrite && _vector2YProp.CanWrite)
                        {
                            _vector2XProp.SetValue(__result, 0f, BindingFlags.Default, null, null, null);
                            _vector2YProp.SetValue(__result, 0f, BindingFlags.Default, null, null, null);
                        }
                    }
                }
#if IGAMEPAD_DEBUG
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Error in Postfix_Vector2ReadValue: {ex.Message}");
                }
#else
                catch
                {
                }
#endif
            }
        }

        private static void Postfix_AxisReadValue(object __instance, ref float __result)
        {
            if (__instance == null)
                return;

            // Verify the axis control is properly attached to a device before proceeding
            try
            {
                if (_deviceProp != null)
                {
                    object device = _deviceProp.GetValue(__instance, BindingFlags.Default, null, null, null);
                    if (device == null)
                        return; // Control not attached to a device, skip
                }
            }
            catch
            {
                return; // If we can't check device, skip to be safe
            }

            string path = ExtractAndNormalizePath(__instance);
#if IGAMEPAD_DEBUG
            ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Postfix_AxisReadValue extracted path: {path}, value: {__result}");
#endif

            // Try to identify if this axis belongs to a stick and which one by comparing the parent stick's axes
            if (!string.IsNullOrEmpty(path))
            {
                string adjustedPath = path;

                try
                {
                    // Get the gamepad instance to access stick controls
                    object gamepadInstance = GetGamepadInstance(_targetGamepadIndex);
                    if (gamepadInstance != null)
                    {
                        // Check if this axis is part of leftStick
                        if (_gamepadLeftStickProp != null)
                        {
                            object leftStick = _gamepadLeftStickProp.GetValue(gamepadInstance, BindingFlags.Default, null, null, null);
                            if (leftStick != null)
                            {
                                PropertyInfo leftStickXProp = leftStick.GetType().GetProperty("x", BindingFlags.Public | BindingFlags.Instance);
                                PropertyInfo leftStickYProp = leftStick.GetType().GetProperty("y", BindingFlags.Public | BindingFlags.Instance);

                                object leftStickX = leftStickXProp?.GetValue(leftStick, BindingFlags.Default, null, null, null);
                                object leftStickY = leftStickYProp?.GetValue(leftStick, BindingFlags.Default, null, null, null);

                                // Compare by reference to see if __instance is the x or y of leftStick
                                if (__instance == leftStickX || __instance == leftStickY)
                                {
                                    adjustedPath = path.Replace("/gamepad/x", "/gamepad/leftstick/x").Replace("/gamepad/y", "/gamepad/leftstick/y");
#if IGAMEPAD_DEBUG
                                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Identified as LEFT STICK axis, adjusted path: {adjustedPath}");
#endif
                                }
                            }
                        }

                        // If not left stick, check if it's part of rightStick
                        if (adjustedPath == path && _gamepadRightStickProp != null)
                        {
                            object rightStick = _gamepadRightStickProp.GetValue(gamepadInstance, BindingFlags.Default, null, null, null);
                            if (rightStick != null)
                            {
                                PropertyInfo rightStickXProp = rightStick.GetType().GetProperty("x", BindingFlags.Public | BindingFlags.Instance);
                                PropertyInfo rightStickYProp = rightStick.GetType().GetProperty("y", BindingFlags.Public | BindingFlags.Instance);

                                object rightStickX = rightStickXProp?.GetValue(rightStick, BindingFlags.Default, null, null, null);
                                object rightStickY = rightStickYProp?.GetValue(rightStick, BindingFlags.Default, null, null, null);

                                // Compare by reference to see if __instance is the x or y of rightStick
                                if (__instance == rightStickX || __instance == rightStickY)
                                {
                                    adjustedPath = path.Replace("/gamepad/x", "/gamepad/rightstick/x").Replace("/gamepad/y", "/gamepad/rightstick/y");
#if IGAMEPAD_DEBUG
                                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Identified as RIGHT STICK axis, adjusted path: {adjustedPath}");
#endif
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Error identifying stick axis: {ex.Message}");
                }

#if IGAMEPAD_DEBUG
                ExplorerCore.LogWarning($"[IGamepadInputInterceptor] Postfix_AxisReadValue storing: {adjustedPath} = {__result}");
#endif
                _axisValues[adjustedPath] = __result;
                // Block input if interception is enabled
                if (_isEnabled && FreeCamPanel.ShouldOverrideInput())
                {
#if IGAMEPAD_DEBUG
                    ExplorerCore.LogWarning($"[IGamepadInputInterceptor] BLOCKING Postfix_AxisReadValue: {adjustedPath} (was {__result}, now 0f)");
#endif
                    __result = 0f;
                }
            }
        }
    }
}
