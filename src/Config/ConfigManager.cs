using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.Config
{
    public static class ConfigManager
    {
        internal static readonly Dictionary<string, IConfigElement> ConfigElements = new();
        internal static readonly Dictionary<string, IConfigElement> InternalConfigs = new();

        // Each Mod Loader has its own ConfigHandler.
        // See the UnityExplorer.Loader namespace for the implementations.
        public static ConfigHandler Handler { get; private set; }

        // Actual CUE Settings
        public static ConfigElement<KeyCode> Master_Toggle;
        public static ConfigElement<bool> Hide_On_Startup;
        public static ConfigElement<float> Startup_Delay_Time;
        public static ConfigElement<bool> Disable_EventSystem_Override;
        public static ConfigElement<int> Target_Display;
        public static ConfigElement<bool> Force_Unlock_Mouse;
        public static ConfigElement<KeyCode> Force_Unlock_Toggle;
        public static ConfigElement<string> Default_Output_Path;
        public static ConfigElement<string> DnSpy_Path;
        public static ConfigElement<bool> Log_Unity_Debug;
        public static ConfigElement<bool> Log_To_Disk;
        public static ConfigElement<UIManager.VerticalAnchor> Main_Navbar_Anchor;
        public static ConfigElement<KeyCode> World_MouseInspect_Keybind;
        public static ConfigElement<KeyCode> UI_MouseInspect_Keybind;
        public static ConfigElement<string> CSConsole_Assembly_Blacklist;
        public static ConfigElement<string> Reflection_Signature_Blacklist;
        public static ConfigElement<bool> Reflection_Hide_NativeInfoPtrs;
        public static ConfigElement<bool> Auto_Scale_UI;
        public static ConfigElement<bool> Reset_Camera_Transform;
        public static ConfigElement<float> Arrow_Size;
        public static ConfigElement<bool> Advanced_Freecam_Selection;

        public static ConfigElement<KeyCode> Pause;
        public static ConfigElement<KeyCode> Frameskip;
        public static ConfigElement<KeyCode> Screenshot;
        public static ConfigElement<KeyCode> HUD_Toggle;
        public static ConfigElement<KeyCode> Freecam_Toggle;
        public static ConfigElement<KeyCode> Block_Freecam_Movement;
        public static ConfigElement<KeyCode> Toggle_Block_Games_Input;
        public static ConfigElement<KeyCode> Speed_Up_Movement;
        public static ConfigElement<KeyCode> Speed_Down_Movement;
        public static ConfigElement<KeyCode> Forwards_1;
        public static ConfigElement<KeyCode> Forwards_2;
        public static ConfigElement<KeyCode> Backwards_1;
        public static ConfigElement<KeyCode> Backwards_2;
        public static ConfigElement<KeyCode> Left_1;
        public static ConfigElement<KeyCode> Left_2;
        public static ConfigElement<KeyCode> Right_1;
        public static ConfigElement<KeyCode> Right_2;
        public static ConfigElement<KeyCode> Up;
        public static ConfigElement<KeyCode> Down;
        public static ConfigElement<KeyCode> Tilt_Left;
        public static ConfigElement<KeyCode> Tilt_Right;
        public static ConfigElement<KeyCode> Tilt_Reset;
        public static ConfigElement<KeyCode> Increase_FOV;
        public static ConfigElement<KeyCode> Decrease_FOV;
        public static ConfigElement<KeyCode> Reset_FOV;
        public static ConfigElement<KeyCode> Toggle_Animations;

        public static ConfigElement<FreeCamPanel.FreeCameraType> Default_Freecam;
        public static ConfigElement<string> Custom_Components_To_Disable;
        public static ConfigElement<int> Preferred_Target_Camera;

        // internal configs
        internal static InternalConfigHandler InternalHandler { get; private set; }
        internal static readonly Dictionary<UIManager.Panels, ConfigElement<string>> PanelSaveData = new();

        internal static ConfigElement<string> GetPanelSaveData(UIManager.Panels panel)
        {
            if (!PanelSaveData.ContainsKey(panel))
                PanelSaveData.Add(panel, new ConfigElement<string>(panel.ToString(), string.Empty, string.Empty, true));
            return PanelSaveData[panel];
        }

        public static void Init(ConfigHandler configHandler)
        {
            Handler = configHandler;
            Handler.Init();

            InternalHandler = new InternalConfigHandler();
            InternalHandler.Init();

            CreateConfigElements();

            Handler.LoadConfig();
            InternalHandler.LoadConfig();

#if STANDALONE
            if (Loader.Standalone.ExplorerEditorBehaviour.Instance)
                Loader.Standalone.ExplorerEditorBehaviour.Instance.LoadConfigs();
#endif
        }

        internal static void RegisterConfigElement<T>(ConfigElement<T> configElement)
        {
            if (!configElement.IsInternal)
            {
                Handler.RegisterConfigElement(configElement);
                ConfigElements.Add(configElement.Name, configElement);
            }
            else
            {
                InternalHandler.RegisterConfigElement(configElement);
                InternalConfigs.Add(configElement.Name, configElement);
            }
        }

        private static void CreateConfigElements()
        {
            Master_Toggle = new("CinematicUnityExplorer Toggle",
                "The key to enable or disable CinematicUnityExplorer's menu and features.",
                KeyCode.F7);

            Hide_On_Startup = new("Hide On Startup",
                "Should CinematicUnityExplorer be hidden on startup?",
                false);

            Startup_Delay_Time = new("Startup Delay Time",
                "The delay on startup before the UI is created.",
                1f);

            Target_Display = new("Target Display",
                "The monitor index for CinematicUnityExplorer to use, if you have multiple. 0 is the default display, 1 is secondary, etc. " +
                "Restart recommended when changing this setting. Make sure your extra monitors are the same resolution as your primary monitor.",
                0);

            Force_Unlock_Mouse = new("Force Unlock Mouse",
                "Force the Cursor to be unlocked (visible) when the CinematicUnityExplorer menu is open.",
                true);
            Force_Unlock_Mouse.OnValueChanged += (bool value) => UniverseLib.Config.ConfigManager.Force_Unlock_Mouse = value;

            Force_Unlock_Toggle = new("Force Unlock Toggle Key",
                "The keybind to toggle the 'Force Unlock Mouse' setting. Only usable when CinematicUnityExplorer is open.",
                KeyCode.None);

            Disable_EventSystem_Override = new("Disable EventSystem override",
                "If enabled, CinematicUnityExplorer will not override the EventSystem from the game.\n<b>May require restart to take effect.</b>",
                false);
            Disable_EventSystem_Override.OnValueChanged += (bool value) => UniverseLib.Config.ConfigManager.Disable_EventSystem_Override = value;

            Default_Output_Path = new("Default Output Path",
                "The default output path when exporting things from CinematicUnityExplorer.",
                Path.Combine(ExplorerCore.ExplorerFolder, "Output"));

            DnSpy_Path = new("dnSpy Path",
                "The full path to dnSpy.exe (64-bit).",
                @"C:/Program Files/dnspy/dnSpy.exe");

            Main_Navbar_Anchor = new("Main Navbar Anchor",
                "The vertical anchor of the main CinematicUnityExplorer Navbar, in case you want to move it.",
                UIManager.VerticalAnchor.Top);

            Log_Unity_Debug = new("Log Unity Debug",
                "Should UnityEngine.Debug.Log messages be printed to CinematicUnityExplorer's log?",
                false);

            Log_To_Disk = new("Log To Disk",
                "Should CinematicUnityExplorer save log files to the disk?",
                true);

            World_MouseInspect_Keybind = new("World Mouse-Inspect Keybind",
                "Optional keybind to being a World-mode Mouse Inspect.",
                KeyCode.None);

            UI_MouseInspect_Keybind = new("UI Mouse-Inspect Keybind",
                "Optional keybind to begin a UI-mode Mouse Inspect.",
                KeyCode.None);

            CSConsole_Assembly_Blacklist = new("CSharp Console Assembly Blacklist",
                "Use this to blacklist Assembly names from being referenced by the C# Console. Requires a Reset of the C# Console.\n" +
                "Separate each Assembly with a semicolon ';'." +
                "For example, to blacklist Assembly-CSharp, you would add 'Assembly-CSharp;'",
                "");

            Reflection_Signature_Blacklist = new("Member Signature Blacklist",
                "Use this to blacklist certain member signatures if they are known to cause a crash or other issues.\r\n" +
                "Seperate signatures with a semicolon ';'.\r\n" +
                "For example, to blacklist Camera.main, you would add 'UnityEngine.Camera.main;'",
                "");

            Reflection_Hide_NativeInfoPtrs = new("Hide NativeMethodInfoPtr_s and NativeFieldInfoPtr_s",
                "Use this to blacklist NativeMethodPtr_s and NativeFieldInfoPtrs_s from the class inspector, mainly to reduce clutter.\r\n" +
                "For example, this will hide 'Class.NativeFieldInfoPtr_value' for the field 'Class.value'.",
                false);

            Auto_Scale_UI = new("Make the mod UI automatically scale with resolution",
                "Especially useful when running games in high resolutions and you are having a hard time reading the mods menu (requires restart).",
                true);

            Reset_Camera_Transform = new("Reset Camera transform on freecam disable",
                "Reset the camera position and rotation between freecam sessions, so the freecam always starts from the gameplay position and rotation.",
                true);
            
            Arrow_Size = new("Visualizers arrows size",
                "Cam Paths nodes and Lights Manager lights visualizers' arrow size (must be positive) (needs visualizer toggled to reflect changes).",
                1f);

            Advanced_Freecam_Selection = new("Advanced Freecam Selection",
                "Enables certain advanced settings on the Freecam panel, in case the user can't get the freecam to work properly (requires game reset).",
                false);

            Pause = new("Pause",
                "Toggle the pause of the game.",
                KeyCode.PageUp);

            Frameskip = new("Frameskip",
                "Skip a frame when the game is paused.",
                KeyCode.PageDown);

            Screenshot = new("Take a screenshot",
                "Takes a screenshot with the size multiplier specified in the Misc panel.\n" +
                "Saves the screenshot to 'CinematicUnityExplorer\\Screenshots' in png format.",
                KeyCode.None);

            HUD_Toggle = new("HUD Toggle",
                "Toggle the games HUD. If there are elements that are still visible try loading the 'Load HUD elements' button on the Misc panel, and toggle the HUD again.",
                KeyCode.Delete);

            Freecam_Toggle = new("Freecam",
                "Toggles freecamera mode.",
                KeyCode.Insert);

            Block_Freecam_Movement = new("Toggle block Freecam movement",
                "Blocks the freecam from moving when pressing the freecam-related hotkeys.",
                KeyCode.Home);

            Toggle_Block_Games_Input = new("Toggle block games input",
                "Blocks the games input when the the freecam is on.\n" +
                "If you don't see a 'Block games input on Freecam' checkbox on the FreecCam panel then it's not supported for this game.",
                KeyCode.KeypadPeriod);

            Speed_Up_Movement = new("Speed up movement",
                "Maintain this key pressed while moving the camera around to increase the freecam movement speed.",
                KeyCode.LeftShift);

            Speed_Down_Movement = new("Speed down movement",
                "Maintain this key pressed while moving the camera around to decrease the freecam movement speed.",
                KeyCode.LeftAlt);

            Forwards_1 = new("Forwards 1",
                "Move the freecam forwards.",
                KeyCode.W);

            Forwards_2 = new("Forwards 2",
                "Move the freecam forwards, alt key.",
                KeyCode.UpArrow);

            Backwards_1 = new("Backwards 1",
                "Move the freecam backward.",
                KeyCode.S);

            Backwards_2 = new("Backwards 2",
                "Move the freecam backward, alt key.",
                KeyCode.DownArrow);

            Left_1 = new("Left 1",
                "Move the freecam to the left.",
                KeyCode.A);

            Left_2 = new("Left 2",
                "Move the freecam to the left, alt key.",
                KeyCode.LeftArrow);

            Right_1 = new("Right 1",
                "Move the freecam to the right.",
                KeyCode.D);

            Right_2 = new("Right 2",
                "Move the freecam to the right, alt key.",
                KeyCode.RightArrow);

            Up = new("Up",
                "Move the freecam upwards.",
                KeyCode.Space);

            Down = new("Down",
                "Move the freecam down.",
                KeyCode.LeftControl);

            Tilt_Left = new("Tilt left",
                "Tilt the camera to the left.",
                KeyCode.Keypad1);

            Tilt_Right = new("Tilt right",
                "Tilt the camera to the left.",
                KeyCode.Keypad3);

            Tilt_Reset = new("Tilt reset",
                "Resets the tilt the camera.",
                KeyCode.Keypad2);

            Increase_FOV = new("Increase FOV",
                "Increase the field of view of the current freecam.",
                KeyCode.KeypadPlus);

            Decrease_FOV = new("Decrease FOV",
                "Decrease the field of view of the current freecam.",
                KeyCode.KeypadMinus);

            Reset_FOV = new("Reset FOV",
                "Resets the field of view of the current freecam to the original one.",
                KeyCode.KeypadMultiply);

            Toggle_Animations = new("Toggle NPC animations",
                "Toggle NPC animations as selected in the Animator panel.",
                KeyCode.Keypad0);

            Default_Freecam = new("Default Freecam mode",
                "Default type of freecam selected on startup (gets automatically updated with the last type of camera used).",
                FreeCamPanel.FreeCameraType.New);

            Custom_Components_To_Disable = new("Custom components to disable",
                "List of custom components to disable when enabling the freecam (gets automatically updated when editing it from the freecam panel).",
                "");

            Preferred_Target_Camera = new("Preferred Target Camera",
                "The camera that will be targeted by the freecam methods.\n" +
                "Only used when Advanced Freecam Selection is enabled.",
                -1);
        }
    }
}
