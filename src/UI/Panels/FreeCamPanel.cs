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
        public FreeCamPanel(UIBase owner) : base(owner)
        {
        }

        public override string Name => "Freecam";
        public override UIManager.Panels PanelType => UIManager.Panels.Freecam;
        public override int MinWidth => 450;
        public override int MinHeight => 750;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;

        internal static bool inFreeCamMode;
        internal static bool usingGameCamera;
        public static Camera ourCamera;
        public static Camera lastMainCamera;
        internal static FreeCamBehaviour freeCamScript;
        internal static CatmullRom.CatmullRomMover cameraPathMover;

        internal static float desiredMoveSpeed = 5f;

        internal static string lastScene;
        internal static Vector3 originalCameraPosition;
        internal static Quaternion originalCameraRotation;
        internal static float originalCameraFOV;

        internal static Vector3? currentUserCameraPosition;
        internal static Quaternion? currentUserCameraRotation;

        internal static Vector3 previousMousePosition;

        internal static Vector3 lastSetCameraPosition;

        static ButtonRef startStopButton;
        public static Toggle useGameCameraToggle;
        public static Toggle blockFreecamMovementToggle;
        public static Toggle blockGamesInputOnFreecamToggle;
        static InputFieldRef positionInput;
        static InputFieldRef moveSpeedInput;
        static Text followObjectLabel;
        static ButtonRef inspectButton;
        public static Toggle followRotationToggle;
        static bool disabledCinemachine;

        public static bool supportedInput => InputManager.CurrentType == InputType.Legacy;

        static InputFieldRef nearClipPlaneInput;
        static Slider nearClipPlaneSlider;
        static float nearClipPlaneValue;

        static InputFieldRef farClipPlaneInput;
        static Slider farClipPlaneSlider;
        static float farClipPlaneValue;

        public static GameObject followObject = null;
        public static Vector3 followObjectLastPosition = Vector3.zero;
        public static Quaternion followObjectLastRotation = Quaternion.identity;

        private static FreecamCursorUnlocker freecamCursorUnlocker = null;

        public static UnityIGCSConnector connector = new();

        internal static void BeginFreecam()
        {
            inFreeCamMode = true;

            previousMousePosition = IInputManager.MousePosition;

            CacheMainCamera();
            SetupFreeCamera();

            inspectButton.GameObject.SetActive(true);

            UpdateClippingPlanes();

            if (freecamCursorUnlocker == null) freecamCursorUnlocker = new FreecamCursorUnlocker();
            freecamCursorUnlocker.Enable();
        }

        static void CacheMainCamera()
        {
            Camera currentMain = Camera.main;
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
                }
            }
            else
                originalCameraRotation = Quaternion.identity;
        }

        static void SetupFreeCamera()
        {
            if (useGameCameraToggle.isOn)
            {
                if (!lastMainCamera)
                {
                    ExplorerCore.LogWarning($"There is no previous Camera found, reverting to default Free Cam.");
                    useGameCameraToggle.isOn = false;
                }
                else
                {
                    usingGameCamera = true;
                    ourCamera = lastMainCamera;
                    MaybeToggleCinemachine(false);

                    // If the farClipPlaneValue is the default one try to use the one from the gameplay camera
                    if (farClipPlaneValue == 2000){
                        farClipPlaneValue = ourCamera.farClipPlane;
                        farClipPlaneInput.Text = farClipPlaneValue.ToString();
                        // Let the default farClipPlane value exceed the slider max value
                        if (farClipPlaneValue <= farClipPlaneSlider.maxValue)
                            farClipPlaneSlider.value = farClipPlaneValue;
                    }
                }
            }

            if (!useGameCameraToggle.isOn)
            {
                usingGameCamera = false;

                if (lastMainCamera)
                    lastMainCamera.enabled = false;
            }

            if (!ourCamera)
            {
                ourCamera = new GameObject("UE_Freecam").AddComponent<Camera>();
                ourCamera.gameObject.tag = "MainCamera";
                GameObject.DontDestroyOnLoad(ourCamera.gameObject);
                ourCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            if (!freeCamScript)
                freeCamScript = ourCamera.gameObject.AddComponent<FreeCamBehaviour>();

            if (!cameraPathMover)
                cameraPathMover = ourCamera.gameObject.AddComponent<CatmullRom.CatmullRomMover>();

            ourCamera.transform.position = (Vector3)currentUserCameraPosition;
            ourCamera.transform.rotation = (Quaternion)currentUserCameraRotation;

            ourCamera.gameObject.SetActive(true);
            ourCamera.enabled = true;

            string currentScene = SceneManager.GetActiveScene().name;
            if (lastScene != currentScene){
                OnResetPosButtonClicked();
            }
            lastScene = currentScene;
        }

        internal static void EndFreecam()
        {
            inFreeCamMode = false;

            if (usingGameCamera)
            {

                MaybeToggleCinemachine(true);
                ourCamera = null;

                if (lastMainCamera)
                {
                    lastMainCamera.transform.position = originalCameraPosition;
                    lastMainCamera.transform.rotation = originalCameraRotation;
                    lastMainCamera.fieldOfView = originalCameraFOV;
                }
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
                    if (comp.GetActualType().ToString() == "Cinemachine.CinemachineBrain"){
                        comp.enabled = enable;
                        disabledCinemachine = !enable;
                        break;
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

            if (connector.isActive)
                return;

            lastSetCameraPosition = ourCamera.transform.position;
            positionInput.Text = ParseUtility.ToStringForInput<Vector3>(lastSetCameraPosition);
        }

        internal static void UpdateClippingPlanes(){
            if (ourCamera) {
                ourCamera.nearClipPlane = nearClipPlaneValue;
                ourCamera.farClipPlane = farClipPlaneValue;
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

            GameObject toggleObj = UIFactory.CreateToggle(ContentRoot, "UseGameCameraToggle", out useGameCameraToggle, out Text useGameCameraText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);
            useGameCameraToggle.onValueChanged.AddListener(OnUseGameCameraToggled);
            useGameCameraToggle.isOn = ConfigManager.Default_Gameplay_Freecam.Value;
            useGameCameraText.text = "Use Game Camera?";

            AddSpacer(5);

            GameObject posRow = AddInputField("Position", "Freecam Pos:", "eg. 0 0 0", out positionInput, PositionInput_OnEndEdit);

            ButtonRef resetPosButton = UIFactory.CreateButton(posRow, "ResetButton", "Reset");
            UIFactory.SetLayoutElement(resetPosButton.GameObject, minWidth: 70, minHeight: 25);
            resetPosButton.OnClick += OnResetPosButtonClicked;

            AddSpacer(5);

            AddInputField("MoveSpeed", "Move Speed:", "Default: 1", out moveSpeedInput, MoveSpeedInput_OnEndEdit);
            moveSpeedInput.Text = desiredMoveSpeed.ToString();

            AddSpacer(5);

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

            followObjectLabel = UIFactory.CreateLabel(ContentRoot, "CurrentFollowObject", "Not following any object.");
            UIFactory.SetLayoutElement(followObjectLabel.gameObject, minWidth: 100, minHeight: 25);

            GameObject followObjectRow = UIFactory.CreateHorizontalGroup(ContentRoot, $"FollowObjectRow", false, false, true, true, 3, default, new(1, 1, 1, 0));

            ButtonRef followButton = UIFactory.CreateButton(followObjectRow, "FollowButton", "Follow GameObject");
            UIFactory.SetLayoutElement(followButton.GameObject, minWidth: 150, minHeight: 25, flexibleWidth: 9999);
            followButton.OnClick += FollowButton_OnClick;

            ButtonRef releaseFollowButton = UIFactory.CreateButton(followObjectRow, "ReleaseFollowButton", "Release Follow GameObject");
            UIFactory.SetLayoutElement(releaseFollowButton.GameObject, minWidth: 150, minHeight: 25, flexibleWidth: 9999);
            releaseFollowButton.OnClick += ReleaseFollowButton_OnClick;

            GameObject followRotationGameObject = UIFactory.CreateToggle(ContentRoot, "followRotationToggle", out followRotationToggle, out Text followRotationText);
            UIFactory.SetLayoutElement(followRotationGameObject, minHeight: 25, flexibleWidth: 9999);
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

        GameObject AddInputField(string name, string labelText, string placeHolder, out InputFieldRef inputField, Action<string> onInputEndEdit)
        {
            GameObject row = UIFactory.CreateHorizontalGroup(ContentRoot, $"{name}_Group", false, false, true, true, 3, default, new(1, 1, 1, 0));

            Text posLabel = UIFactory.CreateLabel(row, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(posLabel.gameObject, minWidth: 100, minHeight: 25);

            inputField = UIFactory.CreateInputField(row, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, minWidth: 50, minHeight: 25, flexibleWidth: 9999);
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
            CamPaths CamPathsPanel = UIManager.GetPanel<CamPaths>(UIManager.Panels.CamPaths);

            if (followObject != null){
                CamPathsPanel.TranslatePointsToGlobal(followRotationToggle.isOn);
            }

            followObject = obj;
            followObjectLastPosition = followObject.transform.position;
            followObjectLastRotation = followObject.transform.rotation;
            followObjectLabel.text = $"Following: {obj.name}";
            
            CamPathsPanel.UpdatedFollowObject(obj);

            CamPathsPanel.TranslatePointsToLocal(followRotationToggle.isOn);

            CamPathsPanel.MaybeRedrawPath();
        }

        void FollowButton_OnClick()
        {
            MouseInspector.Instance.StartInspect(MouseInspectMode.World, FollowObjectAction);
        }

        void ReleaseFollowButton_OnClick()
        {
            if (followObject){
                followObject = null;
                followObjectLastPosition = Vector3.zero;
                followObjectLastRotation = Quaternion.identity;
                followObjectLabel.text = "Not following any object";
            }
            CamPaths CamPathsPanel = UIManager.GetPanel<CamPaths>(UIManager.Panels.CamPaths);

            CamPathsPanel.TranslatePointsToGlobal(followRotationToggle.isOn);

            CamPathsPanel.UpdatedFollowObject(null);
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

        public static bool ShouldOverrideInput(){
            return inFreeCamMode && blockGamesInputOnFreecamToggle.isOn;
        }

        // Getters and Setters for camera position and rotation
        public static Vector3 GetCameraPosition(bool isAbsolute = false){
            if (isAbsolute) return ourCamera.transform.position;
            if (followObject){
                if (followRotationToggle.isOn){
                    return Quaternion.Inverse(followObject.transform.rotation) * (ourCamera.transform.position - followObject.transform.position);
                }
                else {
                    return ourCamera.transform.position - followObject.transform.position;
                }
            }
            return ourCamera.transform.position;
        }

        public static Quaternion GetCameraRotation(bool isAbsolute = false){
            if (isAbsolute) return ourCamera.transform.rotation;
            if (followObject && followRotationToggle.isOn) return Quaternion.Inverse(followObjectLastRotation) * ourCamera.transform.rotation;
            return ourCamera.transform.rotation;
        }

        public static void SetCameraPosition(Vector3 newPosition, bool isAbsolute = false){
            if (isAbsolute){
                ourCamera.transform.position = newPosition;
            }
            else if (followObject){
                if (followRotationToggle.isOn){
                    ourCamera.transform.position = followObject.transform.rotation * newPosition + followObject.transform.position;
                }
                else {
                    ourCamera.transform.position = newPosition + followObject.transform.position;
                }
            }
            else {
                ourCamera.transform.position = newPosition;
            }
        }

        public static void SetCameraRotation(Quaternion newRotation, bool isAbsolute = false){
            if (isAbsolute){
                ourCamera.transform.rotation = newRotation;
            }
            else if (followObject && followRotationToggle.isOn){
                ourCamera.transform.rotation = followObjectLastRotation * newRotation;
            }
            else {
                ourCamera.transform.rotation = newRotation;
            }
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

        internal void Update()
        {
            if (FreeCamPanel.inFreeCamMode)
            {
                if (!FreeCamPanel.ourCamera)
                {
                    FreeCamPanel.EndFreecam();
                    return;
                }

                Transform transform = FreeCamPanel.ourCamera.transform;

                if (!FreeCamPanel.blockFreecamMovementToggle.isOn && !FreeCamPanel.cameraPathMover.playingPath && !FreeCamPanel.connector.isActive) {
                    ProcessInput();
                }

                if (FreeCamPanel.followObject != null){          
                    // position update
                    transform.position += FreeCamPanel.followObject.transform.position - FreeCamPanel.followObjectLastPosition;

                    if (FreeCamPanel.followRotationToggle.isOn){
                        // rotation update
                        Quaternion deltaRotation = FreeCamPanel.followObject.transform.rotation * Quaternion.Inverse(FreeCamPanel.followObjectLastRotation);
                        Vector3 offset = transform.position - FreeCamPanel.followObject.transform.position;
                        transform.position = transform.position - offset + deltaRotation * offset;
                        transform.rotation = deltaRotation * transform.rotation;
                    }

                    FreeCamPanel.followObjectLastPosition = FreeCamPanel.followObject.transform.position;
                    FreeCamPanel.followObjectLastRotation = FreeCamPanel.followObject.transform.rotation;
                }

                FreeCamPanel.connector.executeCameraCommand(FreeCamPanel.ourCamera);

                FreeCamPanel.UpdatePositionInput();
            }
        }

        internal void ProcessInput(){
            FreeCamPanel.currentUserCameraPosition = transform.position;
            FreeCamPanel.currentUserCameraRotation = transform.rotation;

            float moveSpeed = FreeCamPanel.desiredMoveSpeed * 0.01665f; //"0.01665f" (60fps) in place of Time.DeltaTime. DeltaTime causes issues when game is paused.
            float speedModifier = 1;
            if (IInputManager.GetKey(ConfigManager.Speed_Up_Movement.Value))
                speedModifier = 10f;

            if (IInputManager.GetKey(ConfigManager.Speed_Down_Movement.Value))
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

            if (IInputManager.GetKey(ConfigManager.Tilt_Left.Value))
                transform.Rotate(0, 0, moveSpeed * 10, Space.Self);

            if (IInputManager.GetKey(ConfigManager.Tilt_Right.Value))
                transform.Rotate(0, 0, - moveSpeed * 10, Space.Self);

            if (IInputManager.GetKey(ConfigManager.Tilt_Reset.Value)){
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

            if (IInputManager.GetKey(ConfigManager.Decrease_FOV.Value))
            {
                FreeCamPanel.ourCamera.fieldOfView -= moveSpeed; 
            }

            if (IInputManager.GetKey(ConfigManager.Increase_FOV.Value))
            {
                FreeCamPanel.ourCamera.fieldOfView += moveSpeed; 
            }

            if (IInputManager.GetKey(ConfigManager.Reset_FOV.Value)){
                FreeCamPanel.ourCamera.fieldOfView = FreeCamPanel.usingGameCamera ? FreeCamPanel.originalCameraFOV : 60;
            }

            FreeCamPanel.previousMousePosition = IInputManager.MousePosition;
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
