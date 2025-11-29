using UnityExplorer.Config;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UniverseLib.Input;
#if CPP
#if UNHOLLOWER
using UnhollowerRuntimeLib;
#else
using Il2CppInterop.Runtime.Injection;
#endif
#endif
using System;


namespace UnityExplorer
{
    public class ExplorerBehaviour : MonoBehaviour
    {
        internal static ExplorerBehaviour Instance { get; private set; }

#if CPP
        public ExplorerBehaviour(System.IntPtr ptr) : base(ptr) { }
#endif

        internal static void Setup()
        {
#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<ExplorerBehaviour>();
#endif

            GameObject obj = new("ExplorerBehaviour");
            DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<ExplorerBehaviour>();
        }

        internal void Update()
        {
            ExplorerCore.Update();
        }

        // For editor, to clean up objects

        internal void OnDestroy()
        {
            OnApplicationQuit();
        }

        internal bool quitting;

        internal void OnApplicationQuit()
        {
            if (quitting) return;
            quitting = true;
            if (UIManager.UIRoot)
                TryDestroy(UIManager.UIRoot.transform.root.gameObject);

            TryDestroy((typeof(Universe).Assembly.GetType("UniverseLib.UniversalBehaviour")
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null, null)
                as Component).gameObject);

            TryDestroy(this.gameObject);
        }

        internal void TryDestroy(GameObject obj)
        {
            try
            {
                if (obj)
                    Destroy(obj);
            }
            catch { }
        }
    }

    // Cinematic stuff

    public class KeypressListener : MonoBehaviour
    {
        internal static KeypressListener Instance { get; private set; }

#if CPP
        public KeypressListener(System.IntPtr ptr) : base(ptr) { }
#endif
        bool frameSkip;

        internal static void Setup()
        {
#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<KeypressListener>();
#endif

            GameObject obj = new("KeypressListener");
            DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<KeypressListener>();
        }

        public void Update()
        {
            // Continous checks and actions
            stopFrameSkip();
            maybeForcePause();
            maybeTakeScreenshot();

            if (IInputManager.GetKeyDown(ConfigManager.Pause.Value))
            {
                UIManager.GetTimeScaleWidget().PauseToggle();
            }

            // FrameSkip
            if (IInputManager.GetKeyDown(ConfigManager.Frameskip.Value))
            {
                if (UIManager.GetTimeScaleWidget().IsPaused()) {
                    UIManager.GetTimeScaleWidget().PauseToggle();
                    frameSkip = true;
                }
            }

            if (IInputManager.GetKeyDown(ConfigManager.Screenshot.Value))
            {
                UIManager.GetPanel<UnityExplorer.UI.Panels.Misc>(UIManager.Panels.Misc).screenshotStatus = UnityExplorer.UI.Panels.Misc.ScreenshotState.TurnOffUI;
            }

            if (IInputManager.GetKeyDown(ConfigManager.HUD_Toggle.Value))
            {
                UIManager.GetPanel<UnityExplorer.UI.Panels.Misc>(UIManager.Panels.Misc).ToggleHUDElements();
            }

            if (IInputManager.GetKeyDown(ConfigManager.Freecam_Toggle.Value))
            {
                FreeCamPanel.ToggleFreecam();
            }

            if (IInputManager.GetKeyDown(ConfigManager.Block_Freecam_Movement.Value))
            {
                FreeCamPanel.blockFreecamMovementToggle.isOn = !FreeCamPanel.blockFreecamMovementToggle.isOn;
            }

            if (IInputManager.GetKeyDown(ConfigManager.Toggle_Animations.Value))
            {
                UIManager.GetPanel<UnityExplorer.UI.Panels.AnimatorPanel>(UIManager.Panels.AnimatorPanel).HotkeyToggleAnimators();
            }

            if (FreeCamPanel.supportedInput && IInputManager.GetKeyDown(ConfigManager.Toggle_Block_Games_Input.Value))
            {
                FreeCamPanel.blockGamesInputOnFreecamToggle.isOn = !FreeCamPanel.blockGamesInputOnFreecamToggle.isOn;
            }

            // Gamepad controls
            bool xButtonPressed = IGamepadInputInterceptor.IsButtonPressed("/gamepad/buttonwest");
            bool leftStickButton = IGamepadInputInterceptor.IsButtonPressed("/gamepad/leftstickpress");
            bool rightStickButton = IGamepadInputInterceptor.IsButtonPressed("/gamepad/rightstickpress");

            // L3 + X: Toggle time scale override
            if (xButtonPressed && leftStickButton)
            {
                if (IGamepadInputInterceptor.WasButtonPressedThisFrame("/gamepad/buttonwest") ||
                    IGamepadInputInterceptor.WasButtonPressedThisFrame("/gamepad/leftstickpress"))
                {
                    UIManager.GetTimeScaleWidget().ToggleOverride();
                }
            }
            // R3 + X: Toggle freecam
            else if (xButtonPressed && rightStickButton)
            {
                if (IGamepadInputInterceptor.WasButtonPressedThisFrame("/gamepad/buttonwest") ||
                    IGamepadInputInterceptor.WasButtonPressedThisFrame("/gamepad/rightstickpress"))
                {
                    FreeCamPanel.ToggleFreecam();
                }
            }
        }

        void stopFrameSkip(){
            if (frameSkip && !UIManager.GetTimeScaleWidget().IsPaused()){
                frameSkip = false;
                UIManager.GetTimeScaleWidget().PauseToggle();
            }
        }

        void maybeForcePause(){
            // Force pause no matter the game timescale changes
            TimeScaleWidget timescale = UIManager.GetTimeScaleWidget();
            if (timescale != null && timescale.IsPaused() && Time.timeScale != 0) {
                timescale.SetTimeScale(0f);
            }
        }

        void maybeTakeScreenshot(){
            UnityExplorer.UI.Panels.Misc miscPanel = UIManager.GetPanel<UnityExplorer.UI.Panels.Misc>(UIManager.Panels.Misc);
            if (miscPanel != null){
                miscPanel.MaybeTakeScreenshot();
            }
        }
    }
}
