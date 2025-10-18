#if STANDALONE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.Config;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib;

namespace UnityExplorer.Loader.Standalone
{
    public class ExplorerEditorBehaviour : MonoBehaviour
    {
        internal static ExplorerEditorBehaviour Instance { get; private set; }

        public bool Hide_On_Startup = true;
        public KeyCode Master_Toggle_Key = KeyCode.F7;
        public UIManager.VerticalAnchor Main_Navbar_Anchor = UIManager.VerticalAnchor.Top;
        public bool Log_Unity_Debug = false;
        public float Startup_Delay_Time = 1f;
        public KeyCode World_MouseInspect_Keybind;
        public KeyCode UI_MouseInspect_Keybind;
        public bool Force_Unlock_Mouse = true;
        public KeyCode Force_Unlock_Toggle;
        public bool Disable_EventSystem_Override;
        public bool Auto_Scale_UI;
        public bool Reset_Camera_Transform;
        public FreeCamPanel.FreeCameraType Default_Freecam;
        public string Custom_Components_To_Disable;
        public string Preferred_Target_Camera;
        public float Arrow_Size = 1f;
        public bool Freecam_Camera_Target_Selection;

        public KeyCode Pause;
        public KeyCode Frameskip;
        public KeyCode Screenshot;
        public KeyCode HUD_Toggle;
        public KeyCode Freecam_Toggle;
        public KeyCode Block_Freecam_Movement;
        public KeyCode Toggle_Block_Games_Input;
        public KeyCode Speed_Up_Movement;
        public KeyCode Speed_Down_Movement;
        public KeyCode Forwards_1;
        public KeyCode Forwards_2;
        public KeyCode Backwards_1;
        public KeyCode Backwards_2;
        public KeyCode Left_1;
        public KeyCode Left_2;
        public KeyCode Right_1;
        public KeyCode Right_2;
        public KeyCode Up;
        public KeyCode Down;
        public KeyCode Tilt_Left;
        public KeyCode Tilt_Right;
        public KeyCode Tilt_Reset;
        public KeyCode Increase_FOV;
        public KeyCode Decrease_FOV;
        public KeyCode Reset_FOV;
        public KeyCode Toggle_Animations;

        internal void Awake()
        {
            Instance = this;

            ExplorerEditorLoader.Initialize();
            DontDestroyOnLoad(this);
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        internal void OnApplicationQuit()
        {
            Destroy(this.gameObject);
        }
    
        internal void LoadConfigs()
        {
            ConfigManager.Hide_On_Startup.Value = this.Hide_On_Startup;
            ConfigManager.Master_Toggle.Value = this.Master_Toggle_Key;
            ConfigManager.Main_Navbar_Anchor.Value = this.Main_Navbar_Anchor;
            ConfigManager.Log_Unity_Debug.Value = this.Log_Unity_Debug;
            ConfigManager.Startup_Delay_Time.Value = this.Startup_Delay_Time;
            ConfigManager.World_MouseInspect_Keybind.Value = this.World_MouseInspect_Keybind;
            ConfigManager.UI_MouseInspect_Keybind.Value = this.UI_MouseInspect_Keybind;
            ConfigManager.Force_Unlock_Mouse.Value = this.Force_Unlock_Mouse;
            ConfigManager.Force_Unlock_Toggle.Value = this.Force_Unlock_Toggle;
            ConfigManager.Disable_EventSystem_Override.Value = this.Disable_EventSystem_Override;
            ConfigManager.Auto_Scale_UI.Value = this.Auto_Scale_UI;
            ConfigManager.Reset_Camera_Transform.Value = this.Reset_Camera_Transform;
            ConfigManager.Default_Freecam.Value = this.Default_Freecam;
            ConfigManager.Custom_Components_To_Disable.Value = this.Custom_Components_To_Disable;
            ConfigManager.Preferred_Target_Camera.Value = this.Preferred_Target_Camera;

            ConfigManager.Arrow_Size.Value = this.Arrow_Size;
            ConfigManager.Freecam_Camera_Target_Selection.Value = this.Freecam_Camera_Target_Selection;

            ConfigManager.Pause.Value = this.Pause;
            ConfigManager.Frameskip.Value = this.Frameskip;
            ConfigManager.Screenshot.Value = this.Screenshot;
            ConfigManager.HUD_Toggle.Value = this.HUD_Toggle;
            ConfigManager.Freecam_Toggle.Value = this.Freecam_Toggle;
            ConfigManager.Block_Freecam_Movement.Value = this.Block_Freecam_Movement;
            ConfigManager.Toggle_Block_Games_Input.Value = this.Toggle_Block_Games_Input;
            ConfigManager.Speed_Up_Movement.Value = this.Speed_Up_Movement;
            ConfigManager.Speed_Down_Movement.Value = this.Speed_Down_Movement;
            ConfigManager.Forwards_1.Value = this.Forwards_1;
            ConfigManager.Forwards_2.Value = this.Forwards_2;
            ConfigManager.Backwards_1.Value = this.Backwards_1;
            ConfigManager.Backwards_2.Value = this.Backwards_2;
            ConfigManager.Left_1.Value = this.Left_1;
            ConfigManager.Left_2.Value = this.Left_2;
            ConfigManager.Right_1.Value = this.Right_1;
            ConfigManager.Right_2.Value = this.Right_2;
            ConfigManager.Up.Value = this.Up;
            ConfigManager.Down.Value = this.Down;
            ConfigManager.Tilt_Left.Value = this.Tilt_Left;
            ConfigManager.Tilt_Right.Value = this.Tilt_Right;
            ConfigManager.Tilt_Reset.Value = this.Tilt_Reset;
            ConfigManager.Increase_FOV.Value = this.Increase_FOV;
            ConfigManager.Decrease_FOV.Value = this.Decrease_FOV;
            ConfigManager.Reset_FOV.Value = this.Reset_FOV;
            ConfigManager.Toggle_Animations.Value = this.Toggle_Animations;
        }
    }
}
#endif