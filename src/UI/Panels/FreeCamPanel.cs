using UnityEngine.SceneManagement;
using UnityExplorer.Config;
using UnityExplorer.Inspectors;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using System.Runtime.InteropServices;
using CinematicUnityExplorer.Cinematic;

#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif
#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif

namespace UnityExplorer.UI.Panels
{
    public class FreeCamPanel : UEPanel
    {
        public enum FreeCameraType {
            ForcedMatrix,
            Cloned,
            Gameplay,
            New,
        }

        public FreeCamPanel(UIBase owner) : base(owner)
        {
            try {
                connector = new();
            }
            catch (Exception ex) {
                ExplorerCore.LogWarning($"Failed to initialize UnityIGCSConnector: {ex}");
            }
        }

        public override string Name => "Freecam";
        public override UIManager.Panels PanelType => UIManager.Panels.Freecam;
        public override int MinWidth => 500;
        public override int MinHeight => 750;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;

        internal static bool inFreeCamMode;
        public static Camera ourCamera;
        public static Camera lastMainCamera;
        public static Camera cameraMatrixOverrider;
        internal static FreeCamBehaviour freeCamScript;
        internal static CatmullRom.CatmullRomMover cameraPathMover;

        internal static float desiredMoveSpeed = 5f;

        internal static string lastScene;
        internal static Vector3 originalCameraPosition;
        internal static Quaternion originalCameraRotation;
        internal static float originalCameraFOV;

        internal static Vector3? currentUserCameraPosition;
        internal static Quaternion? currentUserCameraRotation;
        internal static float currentUserCameraFov;

        internal static Vector3 previousMousePosition;

        internal static Vector3 lastSetCameraPosition;

        static ButtonRef startStopButton;
        public static Dropdown cameraTypeDropdown;
        internal static Dropdown targetCameraDropdown;
        internal static FreeCameraType currentCameraType;
        public static Toggle blockFreecamMovementToggle;
        public static Toggle blockGamesInputOnFreecamToggle;
        static InputFieldRef positionInput;
        static InputFieldRef moveSpeedInput;
        static InputFieldRef componentsToDisableInput;
        static Text followLookAtObjectLabel;
        static ButtonRef inspectButton;
        public static Toggle followRotationToggle;
        static bool disabledCinemachine;
        static bool disabledOrthographic;
        static List<string> stringComponentsToDisable = new();
        
        private class DisableTarget
        {
            public UnityEngine.Object Target { get; private set; }
            public bool IsGameObject { get; private set; }

            public DisableTarget(Behaviour component)
            {
                Target = component;
                IsGameObject = false;
            }

            public DisableTarget(GameObject gameObject)
            {
                Target = gameObject;
                IsGameObject = true;
            }

            public void SetEnabled(bool enable)
            {
                if (IsGameObject)
                {
                    ((GameObject)Target).SetActive(enable);
                }
                else
                {
                    ((Behaviour)Target).enabled = enable;
                }
            }
        }

        static List<DisableTarget> componentsToDisable = new();

        public static bool supportedInput => InputManager.CurrentType == InputType.Legacy || InputManager.CurrentType == InputType.InputSystem;

        static InputFieldRef nearClipPlaneInput;
        static Slider nearClipPlaneSlider;
        static float nearClipPlaneValue;

        static InputFieldRef farClipPlaneInput;
        static Slider farClipPlaneSlider;
        static float farClipPlaneValue;

        public static GameObject followObject = null;
        public static GameObject lookAtObject = null;
        public static Vector3 followObjectLastPosition = Vector3.zero;
        public static Quaternion followObjectLastRotation = Quaternion.identity;

        private static FreecamCursorUnlocker freecamCursorUnlocker = null;

        public static UnityIGCSConnector connector = null;

        internal static void BeginFreecam()
        {
            connector?.UpdateFreecamStatus(true);

            previousMousePosition = IInputManager.MousePosition;
            CacheMainCamera();
            SetupFreeCamera();

            // Need to be done after CacheMainCamera to not trigger targetCameraDropdown onValueChanged
            inFreeCamMode = true;

            inspectButton.GameObject.SetActive(true);

            UpdateClippingPlanes();

            if (freecamCursorUnlocker == null) freecamCursorUnlocker = new FreecamCursorUnlocker();
            freecamCursorUnlocker.Enable();
        }

        private static Camera[] GetAvailableCameras()
        {
            Camera[] cameras = {};
            try
            {
                cameras = Camera.allCameras;
            }
            // Some ILCPP games might not have Camera.allCameras available
            catch {
                cameras = RuntimeHelper.FindObjectsOfTypeAll<Camera>();
            }

            return cameras.Where(c => c.name != "CUE Camera").ToArray();
        }

        private static Camera GetTargetCamera()
        {
            if (!ConfigManager.Freecam_Camera_Target_Selection.Value && !targetCameraDropdown)
            {
                return Camera.main;
            }

            Camera[] cameras = GetAvailableCameras();

            int selectedCameraTargetIndex = -1;

            // If the list of camera was updated since the last time we checked, update the dropdown and select the current main camera if available
            if (!cameras.Select(c => c.name).SequenceEqual(targetCameraDropdown.options.ToArray().Select(c => c.text))) 
            {
                targetCameraDropdown.options.Clear();
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera cam = cameras[i];
                    targetCameraDropdown.options.Add(new Dropdown.OptionData(cam.name));

                    // The user selected a target camera at some point, default to that
                    if (ConfigManager.Preferred_Target_Camera.Value == GetGameObjectPath(cam.gameObject)) {
                        selectedCameraTargetIndex = i;
                    }
                }

                // If couldn't find the user selected camera default to the main camera
                if (selectedCameraTargetIndex == -1)
                {
                    for (int i = 0; i < cameras.Length; i++)
                    {
                        if (cameras[i] == Camera.main)
                        {
                            selectedCameraTargetIndex = i;
                            break;
                        }
                    }
                }

                SetTargetDropdownValueWithoutNotify(selectedCameraTargetIndex);
                targetCameraDropdown.captionText.text = cameras[selectedCameraTargetIndex].name;
            }

            // Fallback to the first camera
            if (targetCameraDropdown.value >= cameras.Length)
            {
                ExplorerCore.LogWarning($"Selected camera index {targetCameraDropdown.value} is out of bounds, resetting to 0.");
                targetCameraDropdown.value = 0; 
            }

            return cameras[targetCameraDropdown.value];
        }

        static void CacheMainCamera()
        {
            Camera currentMain = GetTargetCamera();

            if (currentMain)
            {
                lastMainCamera = currentMain;
                originalCameraPosition = currentMain.transform.position;
                originalCameraRotation = currentMain.transform.rotation;
                originalCameraFOV = currentMain.fieldOfView;

                if (currentUserCameraPosition == null)
                {
                    currentUserCameraPosition = currentMain.transform.position;
                    currentUserCameraRotation = currentMain.transform.rotation;
                    currentUserCameraFov = currentMain.fieldOfView;
                }
            }
            else
                originalCameraRotation = Quaternion.identity;
        }

        static void SetupFreeCamera()
        {
            switch ((FreeCameraType)cameraTypeDropdown.value)
            {
                case FreeCameraType.Gameplay:
                    if (!lastMainCamera)
                    {
                        ExplorerCore.LogWarning($"There is no previous Camera found, reverting to New Free Cam.");
                        cameraTypeDropdown.value = (int)FreeCameraType.New;
                    }
                    else
                    {
                        currentCameraType = FreeCameraType.Gameplay;
                        ourCamera = lastMainCamera;
                        MaybeToggleCinemachine(false);
                        MaybeToggleOrthographic(false);
                        ToggleCustomComponents(false);

                        // If the farClipPlaneValue is the default one try to use the one from the gameplay camera
                        if (farClipPlaneValue == 2000){
                            farClipPlaneValue = ourCamera.farClipPlane;
                            farClipPlaneInput.Text = farClipPlaneValue.ToString();
                            // Let the default farClipPlane value exceed the slider max value
                            if (farClipPlaneValue <= farClipPlaneSlider.maxValue)
                                farClipPlaneSlider.value = farClipPlaneValue;
                        }
                    }
                    break;
                case FreeCameraType.New:
                    currentCameraType = FreeCameraType.New;

                    if (lastMainCamera){
                        lastMainCamera.enabled = false;
                    }

                    ourCamera = new GameObject("CUE Camera").AddComponent<Camera>();
                    ourCamera.gameObject.tag = "MainCamera";
                    GameObject.DontDestroyOnLoad(ourCamera.gameObject);
                    ourCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                        
                    break;
                case FreeCameraType.Cloned:
                    if (!lastMainCamera)
                    {
                        ExplorerCore.LogWarning($"There is no previous Camera found, reverting to New Free Cam.");
                        cameraTypeDropdown.value = (int)FreeCameraType.New;
                    } else {
                        currentCameraType = FreeCameraType.Cloned;

                        ourCamera = GameObject.Instantiate(lastMainCamera);
                        lastMainCamera.enabled = false;
                        MaybeDeleteCinemachine();
                        MaybeToggleOrthographic(false);
                        ToggleCustomComponents(false);

                        // If the farClipPlaneValue is the default one try to use the one from the gameplay camera
                        if (farClipPlaneValue == 2000){
                            farClipPlaneValue = ourCamera.farClipPlane;
                            farClipPlaneInput.Text = farClipPlaneValue.ToString();
                            // Let the default farClipPlane value exceed the slider max value
                            if (farClipPlaneValue <= farClipPlaneSlider.maxValue)
                                farClipPlaneSlider.value = farClipPlaneValue;
                        }
                    }
                    break;
                case FreeCameraType.ForcedMatrix:
                    if (!lastMainCamera)
                    {
                        ExplorerCore.LogWarning($"There is no previous Camera found, reverting to New Free Cam.");
                        cameraTypeDropdown.value = (int)FreeCameraType.New;
                    } else {
                        currentCameraType = FreeCameraType.ForcedMatrix;
                        ourCamera = lastMainCamera;
                        // HDRP might introduce problems when moving the camera when replacing the worldToCameraMatrix,
                        // so we will try to move the real camera as well.
                        MaybeToggleCinemachine(false);
                        MaybeToggleOrthographic(false);
                        ToggleCustomComponents(false);

                        cameraMatrixOverrider = new GameObject("CUE Camera").AddComponent<Camera>();
                        cameraMatrixOverrider.enabled = false;
                        cameraMatrixOverrider.transform.position = lastMainCamera.transform.position;
                        cameraMatrixOverrider.transform.rotation = lastMainCamera.transform.rotation;
                    }

                    break;
                default:
                    ExplorerCore.LogWarning($"Error: Camera type not implemented");
                    break;
            }

            // Fallback in case we couldn't find the main camera for some reason
            if (!ourCamera)
            {
                ourCamera = new GameObject("CUE Camera").AddComponent<Camera>();
                ourCamera.gameObject.tag = "MainCamera";
                GameObject.DontDestroyOnLoad(ourCamera.gameObject);
                ourCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            if (!freeCamScript)
                freeCamScript = ourCamera.gameObject.AddComponent<FreeCamBehaviour>();

            if (!cameraPathMover)
                cameraPathMover = ourCamera.gameObject.AddComponent<CatmullRom.CatmullRomMover>();

            GetFreecam().transform.position = (Vector3)currentUserCameraPosition;
            GetFreecam().transform.rotation = (Quaternion)currentUserCameraRotation;
            SetFOV(currentUserCameraFov);

            ourCamera.gameObject.SetActive(true);
            ourCamera.enabled = true;

            string currentScene = SceneManager.GetActiveScene().name;
            if (lastScene != currentScene || ConfigManager.Reset_Camera_Transform.Value){
                OnResetPosButtonClicked();
            }
            lastScene = currentScene;
        }

        internal static void EndFreecam()
        {
            inFreeCamMode = false;
            connector?.UpdateFreecamStatus(false);

            switch(currentCameraType) {
                case FreeCameraType.Gameplay:
                    MaybeToggleCinemachine(true);
                    MaybeToggleOrthographic(true);
                    ToggleCustomComponents(true);
                    ourCamera = null;

                    if (lastMainCamera)
                    {
                        lastMainCamera.transform.position = originalCameraPosition;
                        lastMainCamera.transform.rotation = originalCameraRotation;
                        lastMainCamera.fieldOfView = originalCameraFOV;
                    }
                    break;
                case FreeCameraType.New:
                    GameObject.Destroy(ourCamera.gameObject);
                    ourCamera = null;
                    break;
                case FreeCameraType.Cloned:
                    GameObject.Destroy(ourCamera.gameObject);
                    ourCamera = null;
                    break;
                case FreeCameraType.ForcedMatrix:
                    MaybeToggleCinemachine(true);
                    MaybeToggleOrthographic(true);
                    ToggleCustomComponents(true);
                    MethodInfo resetCullingMatrixMethod = typeof(Camera).GetMethod("ResetCullingMatrix", new Type[] {});
                    resetCullingMatrixMethod.Invoke(ourCamera, null);

                    ourCamera.ResetWorldToCameraMatrix();
                    ourCamera.ResetProjectionMatrix();
                    ourCamera = null;

                    GameObject.Destroy(cameraMatrixOverrider.gameObject);
                    cameraMatrixOverrider = null;
                    break;
                default:
                    ExplorerCore.LogWarning($"Error: Camera type not implemented");
                    break;
            }

            if (ourCamera)
                ourCamera.gameObject.SetActive(false);
            else
                inspectButton.GameObject.SetActive(false);

            if (freeCamScript)
            {
                GameObject.Destroy(freeCamScript);
                freeCamScript = null;
            }

            if (cameraPathMover)
            {
                GameObject.Destroy(cameraPathMover);
                cameraPathMover = null;
            }

            if (lastMainCamera)
                lastMainCamera.enabled = true;

            freecamCursorUnlocker.Disable();
        }

        internal static void MaybeResetFreecam()
        {
            if (inFreeCamMode) {
                EndFreecam();
                BeginFreecam();
            }
        }

        internal static void UpdateTargetCameraAction(int newCameraIndex)
        {
            Camera[] cameras = GetAvailableCameras();
            Camera cam = cameras[newCameraIndex];
            ConfigManager.Preferred_Target_Camera.Value = GetGameObjectPath(cam.gameObject);
            MaybeResetFreecam();
        }

        internal static void SetTargetDropdownValueWithoutNotify(int selectedCameraTargetIndex)
        {
            // Some build types don't have a reference to Dropdown.SetValueWithoutNotify
            MethodInfo SetValueWithoutNotifyMethod = targetCameraDropdown.GetType().GetMethod("SetValueWithoutNotify", new[] { typeof(int) });
            if (SetValueWithoutNotifyMethod != null)
            {
                SetValueWithoutNotifyMethod.Invoke(targetCameraDropdown, new object[] { selectedCameraTargetIndex });
            }
            else
            {
                targetCameraDropdown.onValueChanged.RemoveListener(UpdateTargetCameraAction);
                targetCameraDropdown.value = selectedCameraTargetIndex;
                targetCameraDropdown.onValueChanged.AddListener(UpdateTargetCameraAction);
            }
        }

        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        // Experimental feature to automatically disable cinemachine when turning on the gameplay freecam.
        // If it causes problems in some games we should consider removing it or making it a toggle.
        // Also, if there are more generic Unity components that control the camera we should include them here.
        // Not sure if a cinemachine can be inside another gameobject and not in the maincamera component, but we should take that in mind if this doesn't work in some games.
        static void MaybeToggleCinemachine(bool enable){
            // If we want to enable cinemachine but never disabled don't even look for it
            if(enable && !disabledCinemachine)
                return;
            
            if (ourCamera){
                IEnumerable<Behaviour> comps = ourCamera.GetComponentsInChildren<Behaviour>();
                foreach (Behaviour comp in comps)
                {
                    string comp_type = comp.GetActualType().ToString();
                    if (comp_type == "Cinemachine.CinemachineBrain" || comp_type == "Il2CppCinemachine.CinemachineBrain"){
                        comp.enabled = enable;
                        disabledCinemachine = !enable;
                        break;
                    }
                }
            }
        }

        static void MaybeDeleteCinemachine(){
            if (ourCamera){
                IEnumerable<Behaviour> comps = ourCamera.GetComponentsInChildren<Behaviour>();
                foreach (Behaviour comp in comps)
                {
                    string comp_type = comp.GetActualType().ToString();
                    if (comp_type == "Cinemachine.CinemachineBrain" || comp_type == "Il2CppCinemachine.CinemachineBrain"){
                        GameObject.Destroy(comp);
                        break;
                    }
                }
            }
        }

        static void MaybeToggleOrthographic(bool enable){
            if (ourCamera) {
                if (enable) {
                    // Only re-enable orthographic mode if we previously disabled it
                    if (disabledOrthographic) {
                        ourCamera.orthographic = true;
                        disabledOrthographic = false;
                    }
                } else {
                    if (ourCamera.orthographic) {
                        disabledOrthographic = true;
                        ourCamera.orthographic = false;
                    }
                }
            }
        }

        static void SetCameraPositionInput(Vector3 pos)
        {
            if (!ourCamera || lastSetCameraPosition == pos)
                return;

            ourCamera.transform.position = pos;
            lastSetCameraPosition = pos;
        }

        internal static void UpdatePositionInput()
        {
            if (!ourCamera)
                return;

            if (positionInput.Component.isFocused)
                return;

            if (connector != null && connector.IsActive)
                return;

            lastSetCameraPosition = GetFreecam().transform.position;
            positionInput.Text = ParseUtility.ToStringForInput<Vector3>(lastSetCameraPosition);
        }

        internal static void UpdateClippingPlanes(){
            if (ourCamera) {
                ourCamera.nearClipPlane = nearClipPlaneValue;
                ourCamera.farClipPlane = farClipPlaneValue;
            }

            if (cameraMatrixOverrider) {
                cameraMatrixOverrider.nearClipPlane = nearClipPlaneValue;
                cameraMatrixOverrider.farClipPlane = farClipPlaneValue;
            }
        }

        // ~~~~~~~~ UI construction / callbacks ~~~~~~~~

        protected override void ConstructPanelContent()
        {
            startStopButton = UIFactory.CreateButton(ContentRoot, "ToggleButton", "Freecam");
            UIFactory.SetLayoutElement(startStopButton.GameObject, minWidth: 150, minHeight: 25, flexibleWidth: 9999);
            startStopButton.OnClick += StartStopButton_OnClick;
            SetToggleButtonState();

            AddSpacer(5);

            GameObject CameraModeRow = UIFactory.CreateHorizontalGroup(ContentRoot, "CameraModeRow", false, false, true, true, 3, default, new(1, 1, 1, 0));

            Text CameraMode = UIFactory.CreateLabel(CameraModeRow, "Camera Mode", "Camera Mode:");
            UIFactory.SetLayoutElement(CameraMode.gameObject, minWidth: 75, minHeight: 25);

            GameObject cameraTypeDropdownObj = UIFactory.CreateDropdown(CameraModeRow, "CameraType_Dropdown", out cameraTypeDropdown, null, 14, (idx) => {
                ConfigManager.Default_Freecam.Value = (FreeCameraType)idx;
                MaybeResetFreecam();
            });
            foreach (FreeCameraType type in Enum.GetValues(typeof(FreeCameraType)).Cast<FreeCameraType>()) {
                cameraTypeDropdown.options.Add(new Dropdown.OptionData(Enum.GetName(typeof(FreeCameraType), type)));
            }
            UIFactory.SetLayoutElement(cameraTypeDropdownObj, minHeight: 25, minWidth: 150);
            cameraTypeDropdown.value = (int)ConfigManager.Default_Freecam.Value;

            if (ConfigManager.Freecam_Camera_Target_Selection.Value)
            {
                Text TargetCamLabel = UIFactory.CreateLabel(CameraModeRow, "Target_cam_label", " Target cam:");
                UIFactory.SetLayoutElement(TargetCamLabel.gameObject, minWidth: 75, minHeight: 25);

                GameObject targetCameraDropdownObj = UIFactory.CreateDropdown(CameraModeRow, "TargetCamera_Dropdown", out targetCameraDropdown, null, 14, null);
                targetCameraDropdown.onValueChanged.AddListener(UpdateTargetCameraAction);

                try {
                    Camera[] cameras = GetAvailableCameras();
                    foreach (Camera cam in cameras) {
                        targetCameraDropdown.options.Add(new Dropdown.OptionData(cam.name));
                    }
                    if (Camera.main) {
                        SetTargetDropdownValueWithoutNotify(Array.IndexOf(cameras, Camera.main));
                        targetCameraDropdown.captionText.text = Camera.main.name;
                    }
                }
                catch (Exception ex) {
                    ExplorerCore.LogWarning(ex);
                }

                UIFactory.SetLayoutElement(targetCameraDropdownObj, minHeight: 25, minWidth: 150);
            }
            

            GameObject movespeedRow = AddInputField("MoveSpeed", "Move Speed:", "Default: 1", out moveSpeedInput, MoveSpeedInput_OnEndEdit, 85, 25);
            moveSpeedInput.Text = desiredMoveSpeed.ToString();

            AddInputField("Position", "Freecam Pos:", "eg. 0 0 0", out positionInput, PositionInput_OnEndEdit, 100, 175, movespeedRow);

            ButtonRef resetPosButton = UIFactory.CreateButton(movespeedRow, "ResetButton", "Reset");
            UIFactory.SetLayoutElement(resetPosButton.GameObject, minWidth: 70, minHeight: 25);
            resetPosButton.OnClick += OnResetPosButtonClicked;

            Text componentsToDisableLabel = UIFactory.CreateLabel(ContentRoot, $"ComponentsToDisable_Label", "Disable Components/GameObjects:");
            UIFactory.SetLayoutElement(componentsToDisableLabel.gameObject, minWidth: 250, minHeight: 25);

            componentsToDisableInput = UIFactory.CreateInputField(ContentRoot, $"componentsToDisable_Input", "CinemachineBrain");
            UIFactory.SetLayoutElement(componentsToDisableInput.GameObject, minWidth: 50, minHeight: 25, flexibleWidth: 9999);
            componentsToDisableInput.Component.GetOnEndEdit().AddListener(ComponentsToDisableInput_OnEndEdit);
            componentsToDisableInput.Text = ConfigManager.Custom_Components_To_Disable.Value;
            stringComponentsToDisable = ConfigManager.Custom_Components_To_Disable.Value.Split(',').Select(c => c.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

            GameObject controllerRow = UIFactory.CreateHorizontalGroup(ContentRoot, "ControllerRow", false, false, true, true, 3, default, new(1, 1, 1, 0));

            Text ControllerLabel = UIFactory.CreateLabel(controllerRow, "Controller_label", " Controller:");
            UIFactory.SetLayoutElement(ControllerLabel.gameObject, minWidth: 75, minHeight: 25);

            if (InputManager.CurrentType == InputType.InputSystem)
            {
                GameObject controllerDropdownObj = UIFactory.CreateDropdown(controllerRow, "Controller_Dropdown", out Dropdown controllerDropdown, null, 14, (idx) => {
                    IGamepadInputInterceptor.SetTargetGamepad(idx);
                });
                UIFactory.SetLayoutElement(controllerDropdownObj, minHeight: 25, minWidth: 125);
                // Maybe we can dynamically show the number of connected gamepads in the future
                for (int i = 0; i < 4; i++)
                {
                    controllerDropdown.options.Add(new Dropdown.OptionData($"Gamepad {i + 1}"));
                }
            }

            GameObject togglesRow = UIFactory.CreateHorizontalGroup(ContentRoot, "TogglesRow", false, false, true, true, 3, default, new(1, 1, 1, 0));

            GameObject blockFreecamMovement = UIFactory.CreateToggle(togglesRow, "blockFreecamMovement", out blockFreecamMovementToggle, out Text blockFreecamMovementText);
            UIFactory.SetLayoutElement(blockFreecamMovement, minHeight: 25, flexibleWidth: 9999);
            blockFreecamMovementToggle.isOn = false;
            blockFreecamMovementText.text = "Block Freecam movement";

            if (supportedInput){
                GameObject blockGamesInputOnFreecam = UIFactory.CreateToggle(togglesRow, "blockGamesInputOnFreecam", out blockGamesInputOnFreecamToggle, out Text blockGamesInputOnFreecamText);
                UIFactory.SetLayoutElement(blockGamesInputOnFreecam, minHeight: 25, flexibleWidth: 9999);
                blockGamesInputOnFreecamToggle.isOn = true;
                blockGamesInputOnFreecamText.text = "Block games input on Freecam";
            }

            AddSpacer(5);

            GameObject nearCameraClipGroup = AddInputField("NearClipPlane", "Near clip plane:", "0", out nearClipPlaneInput, NearClipInput_OnEndEdit);
            nearClipPlaneInput.Text = nearClipPlaneValue.ToString();

            GameObject nearClipObj = UIFactory.CreateSlider(nearCameraClipGroup, "Camera near plane clip", out nearClipPlaneSlider);
            UIFactory.SetLayoutElement(nearClipObj, minHeight: 25, minWidth: 250, flexibleWidth: 0);
            nearClipPlaneSlider.onValueChanged.AddListener((newNearPlaneClip) => {
                nearClipPlaneValue = newNearPlaneClip;
                nearClipPlaneInput.Text = nearClipPlaneValue.ToString();

                UpdateClippingPlanes();
            });
            // Default value
            nearClipPlaneValue = 0.1f;
            nearClipPlaneSlider.m_FillImage.color = Color.clear;
            nearClipPlaneSlider.minValue = 0.001f;
            nearClipPlaneSlider.maxValue = 100;
            nearClipPlaneSlider.value = 0.1f; // doesn't take nearClipPlaneValue for some reason??

            AddSpacer(5);

            GameObject farCameraClipGroup = AddInputField("FearClipPlane", "Far clip plane:", "0", out farClipPlaneInput, FarClipInput_OnEndEdit);
            farClipPlaneInput.Text = farClipPlaneValue.ToString();

            GameObject farClipObj = UIFactory.CreateSlider(farCameraClipGroup, "Camera far plane clip", out farClipPlaneSlider);
            UIFactory.SetLayoutElement(farClipObj, minHeight: 25, minWidth: 250, flexibleWidth: 0);
            farClipPlaneSlider.onValueChanged.AddListener((newFarPlaneClip) => {
                farClipPlaneValue = newFarPlaneClip;
                farClipPlaneInput.Text = farClipPlaneValue.ToString();

                UpdateClippingPlanes();
            });
            // Default value
            farClipPlaneValue = 2000;
            farClipPlaneSlider.m_FillImage.color = Color.clear;
            farClipPlaneSlider.minValue = 100;
            farClipPlaneSlider.maxValue = 2000;
            farClipPlaneSlider.value = 2000; // doesn't take farClipPlaneValue for some reason??

            AddSpacer(5);

            followLookAtObjectLabel = UIFactory.CreateLabel(ContentRoot, "CurrentFollowLookAtObject", "Not following/looking at any object.");
            UIFactory.SetLayoutElement(followLookAtObjectLabel.gameObject, minWidth: 150, minHeight: 25);

            GameObject followObjectRow = UIFactory.CreateHorizontalGroup(ContentRoot, $"FollowObjectRow", false, false, true, true, 3, default, new(1, 1, 1, 0));

            ButtonRef followButton = UIFactory.CreateButton(followObjectRow, "FollowButton", "Follow GameObject");
            UIFactory.SetLayoutElement(followButton.GameObject, minWidth: 150, minHeight: 25, flexibleWidth: 9999);
            followButton.OnClick += FollowButton_OnClick;

            GameObject followRotationGameObject = UIFactory.CreateToggle(followObjectRow, "followRotationToggle", out followRotationToggle, out Text followRotationText);
            UIFactory.SetLayoutElement(followRotationGameObject, minWidth: 150, minHeight: 25, flexibleWidth: 9999);
            followRotationToggle.isOn = false;
            followRotationText.text = "Follow Object Rotation";
            followRotationToggle.onValueChanged.AddListener((value) => {
                if (followObject != null){
                    CamPaths CamPathsPanel = UIManager.GetPanel<CamPaths>(UIManager.Panels.CamPaths);
                    if (value){
                        CamPathsPanel.TranslatePointsRotationToLocal();
                    }
                    else {
                        CamPathsPanel.TranslatePointsRotationToGlobal();
                    }
                                        
                    CamPathsPanel.MaybeRedrawPath();
                }
            });

            GameObject lookAtObjectRow = UIFactory.CreateHorizontalGroup(ContentRoot, $"LookAtObjectRow", false, false, true, true, 3, default, new(1, 1, 1, 0));

            ButtonRef lookAtButton = UIFactory.CreateButton(lookAtObjectRow, "LookAtButton", "Look At GameObject");
            UIFactory.SetLayoutElement(lookAtButton.GameObject, minWidth: 140, minHeight: 25, flexibleWidth: 9999);
            lookAtButton.OnClick += LookAtButton_OnClick;

            ButtonRef releaseFollowLookAtButton = UIFactory.CreateButton(lookAtObjectRow, "ReleaseFollowLookAtButton", "Release Follow/Look At");
            UIFactory.SetLayoutElement(releaseFollowLookAtButton.GameObject, minWidth: 160, minHeight: 25, flexibleWidth: 9999);
            releaseFollowLookAtButton.OnClick += ReleaseFollowLookAtButton_OnClick;

            AddSpacer(5);

            string instructions = "Controls:\n" +
            $"- {ConfigManager.Forwards_1.Value},{ConfigManager.Backwards_1.Value},{ConfigManager.Left_1.Value},{ConfigManager.Right_1.Value} / {ConfigManager.Forwards_2.Value},{ConfigManager.Backwards_2.Value},{ConfigManager.Left_2.Value},{ConfigManager.Right_2.Value}: Movement\n" +
            $"- {ConfigManager.Up.Value}: Move up\n" +
            $"- {ConfigManager.Down.Value}: Move down\n" +
            $"- {ConfigManager.Tilt_Left.Value} / {ConfigManager.Tilt_Right.Value}: Tilt \n" +
            $"- Right Mouse Button: Free look\n" +
            $"- {ConfigManager.Speed_Up_Movement.Value}: Super speed\n" +
            $"- {ConfigManager.Speed_Down_Movement.Value}: Slow speed\n" +
            $"- {ConfigManager.Increase_FOV.Value} / {ConfigManager.Decrease_FOV.Value}: Change FOV\n" +
            $"- {ConfigManager.Tilt_Reset.Value}: Reset tilt\n" +
            $"- {ConfigManager.Reset_FOV.Value}: Reset FOV\n\n" +
            "Extra:\n" +
            $"- {ConfigManager.Freecam_Toggle.Value}: Freecam toggle\n" +
            $"- {ConfigManager.Block_Freecam_Movement.Value}: Toggle block Freecam\n" +
            (supportedInput ? $"- {ConfigManager.Toggle_Block_Games_Input.Value}: Toggle games input on Freecam\n" : "") +
            $"- {ConfigManager.HUD_Toggle.Value}: HUD toggle\n" +
            $"- {ConfigManager.Pause.Value}: Pause\n" +
            $"- {ConfigManager.Frameskip.Value}: Frameskip\n" +
            $"- {ConfigManager.Toggle_Animations.Value}: Toggle NPC animations\n";

            if (ConfigManager.Frameskip.Value != KeyCode.None) instructions = instructions + $"- {ConfigManager.Screenshot.Value}: Screenshot\n";

            Text instructionsText = UIFactory.CreateLabel(ContentRoot, "Instructions", instructions, TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(instructionsText.gameObject, flexibleWidth: 9999, flexibleHeight: 9999);

            AddSpacer(5);

            inspectButton = UIFactory.CreateButton(ContentRoot, "InspectButton", "Inspect Free Camera");
            UIFactory.SetLayoutElement(inspectButton.GameObject, flexibleWidth: 9999, minHeight: 25);
            inspectButton.OnClick += () => { InspectorManager.Inspect(ourCamera); };
            inspectButton.GameObject.SetActive(false);

            AddSpacer(5);
        }

        void AddSpacer(int height)
        {
            GameObject obj = UIFactory.CreateUIObject("Spacer", ContentRoot);
            UIFactory.SetLayoutElement(obj, minHeight: height, flexibleHeight: 0);
        }

        GameObject AddInputField(string name, string labelText, string placeHolder, out InputFieldRef inputField, Action<string> onInputEndEdit, int minTextWidth = 100, int minInputWidth = 150, GameObject parent = null)
        {
            GameObject row = parent != null ? parent : UIFactory.CreateHorizontalGroup(ContentRoot, $"{name}_Group", false, false, true, true, 3, default, new(1, 1, 1, 0));

            Text posLabel = UIFactory.CreateLabel(row, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(posLabel.gameObject, minWidth: minTextWidth, minHeight: 25);

            inputField = UIFactory.CreateInputField(row, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, minWidth: minInputWidth, minHeight: 25, flexibleWidth: 9999);
            inputField.Component.GetOnEndEdit().AddListener(onInputEndEdit);

            return row;
        }

        public static void StartStopButton_OnClick()
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (inFreeCamMode)
                EndFreecam();
            else
                BeginFreecam();

            SetToggleButtonState();
        }

        public static void FollowObjectAction(GameObject obj){
            ReleaseFollowLookAtButton_OnClick();

            CamPaths CamPathsPanel = UIManager.GetPanel<CamPaths>(UIManager.Panels.CamPaths);

            if (followObject != null){
                CamPathsPanel.TranslatePointsToGlobal(followRotationToggle.isOn);
            }

            followObject = obj;
            followObjectLastPosition = followObject.transform.position;
            followObjectLastRotation = followObject.transform.rotation;
            followLookAtObjectLabel.text = $"Following: {obj.name}";
            
            CamPathsPanel.UpdatedFollowObject(obj);

            CamPathsPanel.TranslatePointsToLocal(followRotationToggle.isOn);

            CamPathsPanel.MaybeRedrawPath();
        }

        void FollowButton_OnClick()
        {
            MouseInspector.Instance.StartInspect(MouseInspectMode.World, FollowObjectAction);
        }

        public static void LookAtObjectAction(GameObject obj){
            ReleaseFollowLookAtButton_OnClick();
            lookAtObject = obj;
            followLookAtObjectLabel.text = $"Looking at: {obj.name}";
        }

        void LookAtButton_OnClick()
        {
            MouseInspector.Instance.StartInspect(MouseInspectMode.World, LookAtObjectAction);
        }

        static void ReleaseFollowLookAtButton_OnClick()
        {
            if (followObject != null){
                CamPaths CamPathsPanel = UIManager.GetPanel<CamPaths>(UIManager.Panels.CamPaths);
                CamPathsPanel.TranslatePointsToGlobal(followRotationToggle.isOn);
                CamPathsPanel.UpdatedFollowObject(null);
            }

            lookAtObject = null;
            followObject = null;
            followObjectLastPosition = Vector3.zero;
            followObjectLastRotation = Quaternion.identity;
            followLookAtObjectLabel.text = "Not following/looking at any object";
        }

        static void SetToggleButtonState()
        {
            if (inFreeCamMode)
            {
                RuntimeHelper.SetColorBlockAuto(startStopButton.Component, new(0.4f, 0.2f, 0.2f));
                startStopButton.ButtonText.text = "End Freecam";
            }
            else
            {
                RuntimeHelper.SetColorBlockAuto(startStopButton.Component, new(0.2f, 0.4f, 0.2f));
                startStopButton.ButtonText.text = "Begin Freecam";
            }
        }

        void OnUseGameCameraToggled(bool value)
        {
            // TODO: Change the value on ConfigManager.Default_Gameplay_Freecam and save it
            EventSystemHelper.SetSelectedGameObject(null);

            if (!inFreeCamMode)
                return;

            EndFreecam();
            BeginFreecam();
        }

        static void OnResetPosButtonClicked()
        {
            currentUserCameraPosition = originalCameraPosition;
            currentUserCameraRotation = originalCameraRotation;

            if (inFreeCamMode && ourCamera)
            {
                SetCameraPosition((Vector3)currentUserCameraPosition, true);
                SetCameraRotation((Quaternion)currentUserCameraRotation, true);
                ourCamera.fieldOfView = originalCameraFOV;
            }

            positionInput.Text = ParseUtility.ToStringForInput<Vector3>(originalCameraPosition);
        }

        void PositionInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out Vector3 parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse position to Vector3: {parseEx.ReflectionExToString()}");
                UpdatePositionInput();
                return;
            }

            SetCameraPositionInput(parsed);
        }

        void MoveSpeedInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                moveSpeedInput.Text = desiredMoveSpeed.ToString();
                return;
            }

            desiredMoveSpeed = parsed;
        }

        void ComponentsToDisableInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            ConfigManager.Custom_Components_To_Disable.Value = input;
            stringComponentsToDisable = input.Split(',').Select(c => c.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        static List<DisableTarget> GetComponentsToDisable()
        {
            List<DisableTarget> components = new();
            if (stringComponentsToDisable == null || stringComponentsToDisable.Count == 0)
            {
                return components;
            }

            foreach (string stringComponent in stringComponentsToDisable)
            {
                List<string> pathToComponent = stringComponent.Split('/').Where(x => !string.IsNullOrEmpty(x)).ToList();
                GameObject currentGameObject = ourCamera.gameObject;
                for (var i = 0; i < pathToComponent.Count; i++)
                {
                    string pathStep = pathToComponent[i];
                    if (i == 0 && pathStep == "~")
                    {
                        // Check if we can find the next steps game object in the path
                        i++;
                        pathStep = pathToComponent[i];
                        GameObject foundNextPathStep = null;
                        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
                            if (obj.name == pathStep)
                            {
                                foundNextPathStep = obj;
                                break;
                            }
                        }

                        if (!foundNextPathStep)
                        {
                            var test = new GameObject("Test CUE object to get DontDestroyOnLoad scene");
                            UnityEngine.Object.DontDestroyOnLoad(test);
                            Scene scene = test.scene;
                            GameObject.Destroy(test);

                            foreach (GameObject obj in scene.GetRootGameObjects()) {
                                if (obj.name == pathStep)
                                {
                                    foundNextPathStep = obj;
                                    break;
                                }
                            }
                        }

                        if (!foundNextPathStep)
                        {
                            for (int j = 0; j < SceneManager.sceneCount; j++)
                            {
                                Scene scene = SceneManager.GetSceneAt(j);
                                if (scene == SceneManager.GetActiveScene() || !scene.isLoaded) continue;

                                foreach (GameObject obj in scene.GetRootGameObjects()) {
                                    if (obj.name == pathStep)
                                    {
                                        foundNextPathStep = obj;
                                        break;
                                    }
                                }
                                if (foundNextPathStep) break;
                            }
                        }

                        if (!foundNextPathStep)
                        {
                            ExplorerCore.LogWarning($"Couldn't find root {foundNextPathStep} gameobject on {stringComponent} path to disable it.");
                            break;
                        }
                        currentGameObject = foundNextPathStep;
                        continue;
                    }

                    if (pathStep == "..") {
                        if (!currentGameObject.transform.parent)
                        {
                            ExplorerCore.LogWarning($"{currentGameObject.name} doesn't have a parent, so can't go up the parent hierarchy on {stringComponent}.");
                            break;
                        }

                        currentGameObject = currentGameObject.transform.parent.gameObject;
                        continue;
                    }

                    // Last member of the path, could be either a component or a GameObject
                    if (i == pathToComponent.Count - 1) {
                        Behaviour comp = GetComponentByName(currentGameObject, pathStep);
                        if (comp) {
                            components.Add(new DisableTarget(comp));
                        }
                        else {
                            Transform nextGameObjectTransform = currentGameObject.transform.Find(pathStep);
                            if (!nextGameObjectTransform) {
                                ExplorerCore.LogWarning($"Couldn't find {pathStep} component or gameobject on {stringComponent} path to disable it.");
                                break;
                            }

                            components.Add(new DisableTarget(nextGameObjectTransform.gameObject));
                        }
                    }
                    else {
                        Transform nextGameObjectTransform = currentGameObject.transform.Find(pathStep);
                        if (!nextGameObjectTransform)
                        {
                            ExplorerCore.LogWarning($"Couldn't find {pathStep} gameobject on {stringComponent} path to disable it.");
                            break;
                        }

                        currentGameObject = nextGameObjectTransform.gameObject;
                    }
                }
            }
            return components;
        }

        static void ToggleCustomComponents(bool enable)
        {
            // If disable get the components again
            if (!enable)
            {
                componentsToDisable = GetComponentsToDisable();
            }

            foreach(DisableTarget target in componentsToDisable)
            {
                // We could outright delete the components/gameobjects if on Cloned freecam mode
                target.SetEnabled(enable);
            }
        }

        static Behaviour GetComponentByName(GameObject obj, string componentsName)
        {
            if (obj)
            {
                IEnumerable<Behaviour> comps = obj.GetComponents<Behaviour>();
                foreach (Behaviour comp in comps)
                {
                    string comp_type = comp.GetActualType().ToString();
                    if (comp_type == componentsName){
                        return comp;
                    }
                }
            }
            return null;
        }

        void NearClipInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                nearClipPlaneInput.Text = nearClipPlaneValue.ToString();
                return;
            }

            nearClipPlaneValue = parsed;
            nearClipPlaneSlider.value = nearClipPlaneValue;

            UpdateClippingPlanes();
        }

        void FarClipInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                farClipPlaneInput.Text = farClipPlaneValue.ToString();
                return;
            }

            farClipPlaneValue = parsed;
            farClipPlaneSlider.value = farClipPlaneValue;

            UpdateClippingPlanes();
        }

        public static bool IsConnectorActive()
        {
            return FreeCamPanel.connector != null && FreeCamPanel.connector.IsActive;
        }

        public static bool ShouldOverrideInput(){
            return inFreeCamMode && blockGamesInputOnFreecamToggle.isOn;
        }

        public static Camera GetFreecam(){
            if (currentCameraType == FreeCameraType.ForcedMatrix) return cameraMatrixOverrider;
            return ourCamera;
        }

        // Getters and Setters for camera position and rotation
        public static Vector3 GetCameraPosition(bool isAbsolute = false){
            Camera freecam = GetFreecam();
            if (isAbsolute) return freecam.transform.position;
            if (followObject){
                if (followRotationToggle.isOn){
                    return Quaternion.Inverse(followObject.transform.rotation) * (freecam.transform.position - followObject.transform.position);
                }
                else {
                    return freecam.transform.position - followObject.transform.position;
                }
            }
            return freecam.transform.position;
        }

        public static Quaternion GetCameraRotation(bool isAbsolute = false){
            Camera freecam = GetFreecam();
            if (isAbsolute) return freecam.transform.rotation;
            if (followObject && followRotationToggle.isOn) return Quaternion.Inverse(followObjectLastRotation) * freecam.transform.rotation;
            return freecam.transform.rotation;
        }

        public static void SetCameraPosition(Vector3 newPosition, bool isAbsolute = false){
            Camera freecam = GetFreecam();
            if (isAbsolute){
                freecam.transform.position = newPosition;
            }
            else if (followObject){
                if (followRotationToggle.isOn){
                    freecam.transform.position = followObject.transform.rotation * newPosition + followObject.transform.position;
                }
                else {
                    freecam.transform.position = newPosition + followObject.transform.position;
                }
            }
            else {
                freecam.transform.position = newPosition;
            }
        }

        public static void SetCameraRotation(Quaternion newRotation, bool isAbsolute = false){
            Camera freecam = GetFreecam();
            if (lookAtObject){
                return;
            }
            if (isAbsolute){
                freecam.transform.rotation = newRotation;
            }
            else if (followObject && followRotationToggle.isOn){
                freecam.transform.rotation = followObjectLastRotation * newRotation;
            }
            else {
                freecam.transform.rotation = newRotation;
            }
        }

        public static void SetFOV(float newFOV){
            GetFreecam().fieldOfView = newFOV;
        }
    }

    internal class FreeCamBehaviour : MonoBehaviour
    {
#if CPP
        static FreeCamBehaviour()
        {
            ClassInjector.RegisterTypeInIl2Cpp<FreeCamBehaviour>();
        }

        public FreeCamBehaviour(IntPtr ptr) : base(ptr) { }
#endif
        private Action onBeforeRenderAction;
        private Vector3 cachedPosition;
        private Quaternion cachedRotation;

        internal void Update()
        {
            if (FreeCamPanel.inFreeCamMode)
            {
                if (!FreeCamPanel.ourCamera)
                {
                    FreeCamPanel.EndFreecam();
                    return;
                }
                Transform movingTransform = FreeCamPanel.GetFreecam().transform;

                if (!FreeCamPanel.blockFreecamMovementToggle.isOn && !FreeCamPanel.cameraPathMover.playingPath && FreeCamPanel.connector?.IsActive != true && !IsInputFieldInFocus()) {
                    ProcessInput(movingTransform);
                }

                if (FreeCamPanel.followObject != null){
                    // position update
                    movingTransform.position += FreeCamPanel.followObject.transform.position - FreeCamPanel.followObjectLastPosition;

                    if (FreeCamPanel.followRotationToggle.isOn){
                        // rotation update
                        Quaternion deltaRotation = FreeCamPanel.followObject.transform.rotation * Quaternion.Inverse(FreeCamPanel.followObjectLastRotation);
                        Vector3 offset = movingTransform.position - FreeCamPanel.followObject.transform.position;
                        movingTransform.position = movingTransform.position - offset + deltaRotation * offset;
                        movingTransform.rotation = deltaRotation * movingTransform.rotation;
                    }

                    FreeCamPanel.followObjectLastPosition = FreeCamPanel.followObject.transform.position;
                    FreeCamPanel.followObjectLastRotation = FreeCamPanel.followObject.transform.rotation;
                }

                if (FreeCamPanel.lookAtObject != null && !FreeCamPanel.IsConnectorActive())
                {
                    movingTransform.LookAt(FreeCamPanel.lookAtObject.transform);
                }

                UpdateRelativeMatrix();
                UpdateRealCamera();

                FreeCamPanel.connector?.ExecuteCameraCommand(FreeCamPanel.GetFreecam());

                FreeCamPanel.UpdatePositionInput();
            }
        }

        private bool IsInputFieldInFocus()
        {
            GameObject currentObject = EventSystemHelper.CurrentEventSystem.currentSelectedGameObject;
            if (currentObject != null)
            {
                UnityEngine.UI.InputField selectedInputField = currentObject.GetComponent<UnityEngine.UI.InputField>();
                if (selectedInputField != null)
                {
                    return selectedInputField.isFocused;
                }
            }
            return false;
        }

        private void OnPreCull()
        {
            UpdateRelativeMatrix();
            UpdateRealCamera();
        }

        private void OnPreRender()
        {
            UpdateRelativeMatrix();
            UpdateRealCamera();
        }

        private void OnCameraPostRender()
        {
            UpdateRelativeMatrix();
            //RestoreRealCameraTransform();
        }

        private void LateUpdate()
        {
            UpdateRelativeMatrix();
            //RestoreRealCameraTransform();
        }

        internal void UpdateRelativeMatrix() {
            if (FreeCamPanel.cameraMatrixOverrider != null){
                try
                {
                    MethodInfo getStereoViewMatrixMethod = typeof(Camera).GetMethod("GetStereoViewMatrix");
                    Matrix4x4 stereoViewMatrixOverrider = getStereoViewMatrixMethod.Invoke(FreeCamPanel.cameraMatrixOverrider, new object[] {0}).TryCast<Matrix4x4>();
                    MethodInfo setStereoViewMatrixMethod = typeof(Camera).GetMethod("SetStereoViewMatrix");
                    setStereoViewMatrixMethod.Invoke(FreeCamPanel.ourCamera, new object[] {0, stereoViewMatrixOverrider});
                }
                catch (Exception ex) {
                    ExplorerCore.LogWarning(ex);
                }

                FreeCamPanel.ourCamera.worldToCameraMatrix = FreeCamPanel.cameraMatrixOverrider.worldToCameraMatrix;

                // Maybe I should use nonJitteredProjectionMatrix instead
                FreeCamPanel.ourCamera.projectionMatrix = FreeCamPanel.cameraMatrixOverrider.projectionMatrix;

                // Could be optimized so to not look up cullingMatrixProperty each frame
                PropertyInfo cullingMatrixProperty = typeof(Camera).GetProperty("cullingMatrix");
                Matrix4x4 cullingMatrixOverrider = cullingMatrixProperty.GetValue(FreeCamPanel.cameraMatrixOverrider, null).TryCast<Matrix4x4>();
                MethodInfo setCullingMatrixMethod = cullingMatrixProperty.GetSetMethod();
                setCullingMatrixMethod.Invoke(FreeCamPanel.ourCamera, new object[] {cullingMatrixOverrider});
            }
        }

        internal void ProcessInput(Transform transform){
            FreeCamPanel.currentUserCameraPosition = transform.position;
            FreeCamPanel.currentUserCameraRotation = transform.rotation;

            var leftStickX = IGamepadInputInterceptor.GetAxisValue("/gamepad/leftstick/x");
            var leftStickY = IGamepadInputInterceptor.GetAxisValue("/gamepad/leftstick/y");
            var rightStickX = IGamepadInputInterceptor.GetAxisValue("/gamepad/rightstick/x");
            var rightStickY = IGamepadInputInterceptor.GetAxisValue("/gamepad/rightstick/y");
            float leftTrigger = IGamepadInputInterceptor.GetAxisValue("/gamepad/lefttrigger");
            float rightTrigger = IGamepadInputInterceptor.GetAxisValue("/gamepad/righttrigger");
            bool yButtonPressed = IGamepadInputInterceptor.IsButtonPressed("/gamepad/buttonnorth");
            bool xButtonPressed = IGamepadInputInterceptor.IsButtonPressed("/gamepad/buttonwest");
            bool aButtonPressed = IGamepadInputInterceptor.IsButtonPressed("/gamepad/buttonsouth");
            bool bButtonPressed = IGamepadInputInterceptor.IsButtonPressed("/gamepad/buttoneast");
            bool dpadUpPressed = IGamepadInputInterceptor.IsButtonPressed("/gamepad/dpad/up");
            bool dpadDownPressed = IGamepadInputInterceptor.IsButtonPressed("/gamepad/dpad/down");
            bool dpadLeftPressed = IGamepadInputInterceptor.IsButtonPressed("/gamepad/dpad/left");
            bool dpadRightPressed = IGamepadInputInterceptor.IsButtonPressed("/gamepad/dpad/right");

            float moveSpeed = FreeCamPanel.desiredMoveSpeed * 0.01665f; //"0.01665f" (60fps) in place of Time.DeltaTime. DeltaTime causes issues when game is paused.
            float speedModifier = 1;
            if (IInputManager.GetKey(ConfigManager.Speed_Up_Movement.Value) || yButtonPressed)
                speedModifier = 10f;

            if (IInputManager.GetKey(ConfigManager.Speed_Down_Movement.Value) || xButtonPressed)
                speedModifier = 0.1f;

            moveSpeed *= speedModifier;

            if (IInputManager.GetKey(ConfigManager.Left_1.Value) || IInputManager.GetKey(ConfigManager.Left_2.Value))
                transform.position += transform.right * -1 * moveSpeed;

            if (IInputManager.GetKey(ConfigManager.Right_1.Value) || IInputManager.GetKey(ConfigManager.Right_2.Value))
                transform.position += transform.right * moveSpeed;

            if (IInputManager.GetKey(ConfigManager.Forwards_1.Value) || IInputManager.GetKey(ConfigManager.Forwards_2.Value))
                transform.position += transform.forward * moveSpeed;

            if (IInputManager.GetKey(ConfigManager.Backwards_1.Value) || IInputManager.GetKey(ConfigManager.Backwards_2.Value))
                transform.position += transform.forward * -1 * moveSpeed;

            if (IInputManager.GetKey(ConfigManager.Up.Value))
                transform.position += transform.up * moveSpeed;

            if (IInputManager.GetKey(ConfigManager.Down.Value))
                transform.position += transform.up * -1 * moveSpeed;

            if (leftStickX != 0 || leftStickY != 0)
            {
                transform.position += transform.right * leftStickX * moveSpeed;
                transform.position += transform.forward * leftStickY * moveSpeed;
            }

            if (leftTrigger > 0)
            {
                transform.position += transform.up * leftTrigger * moveSpeed;
            }

            if (rightTrigger > 0)
            {
                transform.position += transform.up * -rightTrigger * moveSpeed;
            }

            // 90 degrees tilt when pressing the speed down hotkey
            if (IInputManager.GetKey(ConfigManager.Speed_Down_Movement.Value))
            {
                if (IInputManager.GetKeyDown(ConfigManager.Tilt_Left.Value)) {
                    transform.Rotate(0, 0, 90, Space.Self);
                }
                else if (IInputManager.GetKeyDown(ConfigManager.Tilt_Right.Value)) {
                    transform.Rotate(0, 0, - 90, Space.Self);
                }
            }
            else
            {
                if (IInputManager.GetKey(ConfigManager.Tilt_Left.Value) || dpadLeftPressed) {
                    transform.Rotate(0, 0, moveSpeed * 10, Space.Self);
                }
                else if (IInputManager.GetKey(ConfigManager.Tilt_Right.Value) || dpadRightPressed) {
                    transform.Rotate(0, 0, - moveSpeed * 10, Space.Self);
                }
            }

            if (IInputManager.GetKey(ConfigManager.Tilt_Reset.Value) || aButtonPressed){
                // Extract the forward direction of the original quaternion
                Vector3 forwardDirection = transform.rotation * Vector3.forward;
                // Reset the tilt by creating a new quaternion with no tilt
                Quaternion newRotation = Quaternion.LookRotation(forwardDirection, Vector3.up);

                transform.rotation = newRotation;
            }

            if (IInputManager.GetMouseButton(1))
            {
                Vector3 mouseDelta = IInputManager.MousePosition - FreeCamPanel.previousMousePosition;
                
                float newRotationX = transform.localEulerAngles.y + mouseDelta.x * 0.3f;
                float newRotationY = transform.localEulerAngles.x - mouseDelta.y * 0.3f;

                // Block the camera rotation to not go further than looking directly up or down.
                // We give a little extra to the [0, 90] rotation segment to not get the camera rotation stuck.
                // If it doesn't work in some game we should revisit this.
                newRotationY = newRotationY > 180f ? Mathf.Clamp(newRotationY, 270f, 360f) : Mathf.Clamp(newRotationY, -1f, 90.0f);

                transform.localEulerAngles = new Vector3(newRotationY, newRotationX, transform.localEulerAngles.z);
                
                // Apply the rotation changes while maintaining the camera's current roll.
                // Not using this method as it can easily modify the tilt, which is undesired.

                /*float pitch = -mouseDelta.y * speedModifier * Time.deltaTime;
                float yaw = mouseDelta.x * speedModifier * Time.deltaTime;

                Vector3 forwardDirection = transform.rotation * Vector3.forward;
                Vector3 rightDirection = transform.rotation * Vector3.right;
                Vector3 upDirection = transform.rotation * Vector3.up;

                Quaternion pitchRotation = Quaternion.AngleAxis(pitch, rightDirection);
                Quaternion yawRotation = Quaternion.AngleAxis(yaw, upDirection);

                transform.rotation = pitchRotation * yawRotation * transform.rotation;*/
            }

            if (rightStickX != 0 || rightStickY != 0)
            {
                float rotationSpeed = moveSpeed * 30;
                float newRotationX = transform.localEulerAngles.y + rightStickX * rotationSpeed;
                float newRotationY = transform.localEulerAngles.x - rightStickY * rotationSpeed;

                // Block the camera rotation to not go further than looking directly up or down.
                newRotationY = newRotationY > 180f ? Mathf.Clamp(newRotationY, 270f, 360f) : Mathf.Clamp(newRotationY, -1f, 90.0f);

                transform.localEulerAngles = new Vector3(newRotationY, newRotationX, transform.localEulerAngles.z);
            }

            if (IInputManager.GetKey(ConfigManager.Decrease_FOV.Value) || dpadUpPressed)
            {
                FreeCamPanel.GetFreecam().fieldOfView = Mathf.Max(FreeCamPanel.GetFreecam().fieldOfView - moveSpeed * 3, 0.001f);
            }

            if (IInputManager.GetKey(ConfigManager.Increase_FOV.Value) || dpadDownPressed)
            {
                FreeCamPanel.GetFreecam().fieldOfView += moveSpeed * 3; 
            }

            if (IInputManager.GetKey(ConfigManager.Reset_FOV.Value) || bButtonPressed){
                FreeCamPanel.GetFreecam().fieldOfView = FreeCamPanel.currentCameraType == FreeCamPanel.FreeCameraType.New ? 60 : FreeCamPanel.originalCameraFOV;
            }

            FreeCamPanel.previousMousePosition = IInputManager.MousePosition;
        }

        // The following code forces the freecamcam to update before rendering a frame
        protected virtual void Awake()
        {
            onBeforeRenderAction = () => { UpdateRelativeMatrix(); UpdateRealCamera(); };
        }

        protected virtual void OnEnable()
        {
#if CPP
            try
            {
                Application.add_onBeforeRender(onBeforeRenderAction);
            }
            catch (Exception exception)
            {
                ExplorerCore.LogWarning($"Failed to listen to BeforeRender: {exception}");
            }
#endif

            // These doesn't exist for Unity <2017 nor when using HDRP
            Type renderPipelineManagerType = ReflectionUtility.GetTypeByName("RenderPipelineManager");
            if (renderPipelineManagerType != null){
                try {
                    EventInfo beginFrameRenderingEvent = renderPipelineManagerType.GetEvent("beginFrameRendering");
                    if (beginFrameRenderingEvent != null) {
                        beginFrameRenderingEvent.AddEventHandler(null, OnBeforeEvent);
                    }
                }
                catch { }

                try {
                    EventInfo endFrameRenderingEvent = renderPipelineManagerType.GetEvent("endFrameRendering");
                    if (endFrameRenderingEvent != null) {
                        endFrameRenderingEvent.AddEventHandler(null, OnAfterEvent);
                    }
                }
                catch { }

                try {
                    EventInfo beginCameraRenderingEvent = renderPipelineManagerType.GetEvent("beginCameraRendering");
                    if (beginCameraRenderingEvent != null) {
                        beginCameraRenderingEvent.AddEventHandler(null, OnBeforeEvent);
                    }
                }
                catch { }

                try {
                    EventInfo endCameraRenderingEvent = renderPipelineManagerType.GetEvent("endCameraRendering");
                    if (endCameraRenderingEvent != null) {
                        endCameraRenderingEvent.AddEventHandler(null, OnAfterEvent);
                    }
                }
                catch { }

                try {
                    EventInfo beginContextRenderingEvent = renderPipelineManagerType.GetEvent("beginContextRendering");
                    if (beginContextRenderingEvent != null) {
                        beginContextRenderingEvent.AddEventHandler(null, OnBeforeEvent);
                    }
                }
                catch { }

                try {
                    EventInfo endContextRenderingEvent = renderPipelineManagerType.GetEvent("endContextRendering");
                    if (endContextRenderingEvent != null) {
                        endContextRenderingEvent.AddEventHandler(null, OnAfterEvent);
                    }
                }
                catch { }
            }

            try {
                EventInfo onBeforeRenderEvent = typeof(Application).GetEvent("onBeforeRender");
                if (onBeforeRenderEvent != null) {
                    onBeforeRenderEvent.AddEventHandler(null, onBeforeRenderAction);
                }
            }
            catch { }
        }

        protected virtual void OnDisable()
        {
#if CPP
            try
            {
                Application.remove_onBeforeRender(onBeforeRenderAction);
            }
            catch (Exception exception)
            {
                ExplorerCore.LogWarning($"Failed to unlisten from BeforeRender: {exception}");
            }
#endif

            // These doesn't exist for Unity <2017 nor when using HDRP
            Type renderPipelineManagerType = ReflectionUtility.GetTypeByName("RenderPipelineManager");
            if (renderPipelineManagerType != null){
                try {
                    EventInfo beginFrameRenderingEvent = renderPipelineManagerType.GetEvent("beginFrameRendering");
                    if (beginFrameRenderingEvent != null) {
                        beginFrameRenderingEvent.RemoveEventHandler(null, OnBeforeEvent);
                    }
                }
                catch { }
                
                try {
                    EventInfo endFrameRenderingEvent = renderPipelineManagerType.GetEvent("endFrameRendering");
                    if (endFrameRenderingEvent != null) {
                        endFrameRenderingEvent.RemoveEventHandler(null, OnAfterEvent);
                    }
                }
                catch { }
                
                try {
                    EventInfo beginCameraRenderingEvent = renderPipelineManagerType.GetEvent("beginCameraRendering");
                    if (beginCameraRenderingEvent != null) {
                        beginCameraRenderingEvent.RemoveEventHandler(null, OnBeforeEvent);
                    }
                }
                catch { }
                
                try {
                    EventInfo endCameraRenderingEvent = renderPipelineManagerType.GetEvent("endCameraRendering");
                    if (endCameraRenderingEvent != null) {
                        endCameraRenderingEvent.RemoveEventHandler(null, OnAfterEvent);
                    }
                }
                catch { }
                
                try {
                    EventInfo beginContextRenderingEvent = renderPipelineManagerType.GetEvent("beginContextRendering");
                    if (beginContextRenderingEvent != null) {
                        beginContextRenderingEvent.RemoveEventHandler(null, OnBeforeEvent);
                    }
                }
                catch { }
                
                try {
                    EventInfo endContextRenderingEvent = renderPipelineManagerType.GetEvent("endContextRendering");
                    if (endContextRenderingEvent != null) {
                        endContextRenderingEvent.RemoveEventHandler(null, OnAfterEvent);
                    }
                }
                catch { }
            }

            EventInfo onBeforeRenderEvent = typeof(Application).GetEvent("onBeforeRender");
            if (onBeforeRenderEvent != null) {
                onBeforeRenderEvent.RemoveEventHandler(null, onBeforeRenderAction);
            }
        }

        private void OnBeforeEvent(object arg1, Camera[] arg2)
        {
            UpdateRelativeMatrix();
            UpdateRealCamera();
        }

        private void OnAfterEvent(object arg1, Camera[] arg2)
        {
            UpdateRelativeMatrix();
            //RestoreRealCameraTransform();
        }

        // HDRP matrix override ignores us moving the camera unfortunately, so we try to copy over the position of the camera matrix overrider.
        protected void UpdateRealCamera() {
            if (FreeCamPanel.cameraMatrixOverrider != null) {
                cachedPosition = FreeCamPanel.ourCamera.transform.position;
                FreeCamPanel.ourCamera.transform.position = FreeCamPanel.cameraMatrixOverrider.transform.position;
                // We also try to update the rotation in case there are game shaders that use the real camera rotation
                cachedRotation = FreeCamPanel.ourCamera.transform.rotation;
                FreeCamPanel.ourCamera.transform.rotation = FreeCamPanel.cameraMatrixOverrider.transform.rotation;
            }
        }

        protected void RestoreRealCameraTransform() {
            if (FreeCamPanel.cameraMatrixOverrider != null) {
                FreeCamPanel.ourCamera.transform.position = cachedPosition;
                FreeCamPanel.ourCamera.transform.rotation = cachedRotation;
            }
            UpdateRealCamera();
        }
    }

    // Dummy UI class to unlock the cursor when freecam is active but the UI is hidden
    internal class FreecamCursorUnlocker : UIBase
    {
        public FreecamCursorUnlocker() : base("freecam.cursor.unlocker.cinematicunityexplorer", () => { }) { }

        public void Enable(){
            Enabled = true;
        }

        public void Disable(){
            Enabled = false;
        }
    }
}
