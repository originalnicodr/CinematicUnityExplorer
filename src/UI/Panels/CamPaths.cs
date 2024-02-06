using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif
#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif

using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;

namespace UnityExplorer.UI.Panels
{
    public class CamPaths : UEPanel
    {
        public CamPaths(UIBase owner) : base(owner)
        {
            controlPoints = new List<CatmullRom.CatmullRomPoint>();
            followObject = null;
            UINodes = new List<GameObject>();
            pathVisualizer = new GameObject("PathVisualizer");
            time = 10;

            // Timer setup
            startTimer = new System.Timers.Timer(3000);
            startTimer.Elapsed += (source, e) => StartPath();
            startTimer.AutoReset = false;

            // CatmullRom Constants
            alphaCatmullRomSlider = new Slider();
            tensionCatmullRomSlider = new Slider();
        }

        public override string Name => "Cam Paths";
        public override UIManager.Panels PanelType => UIManager.Panels.CamPaths;
        public override int MinWidth => 600;
        public override int MinHeight => 600;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;
		public List<CatmullRom.CatmullRomPoint> controlPoints;
        List<GameObject> UINodes;
        bool closedLoop;
        float time = 10;

        GameObject followObject;

        Toggle visualizePathToggle;
        public GameObject pathVisualizer;

        bool unpauseOnPlay;
        bool waitBeforePlay;
        private System.Timers.Timer startTimer;

        InputFieldRef alphaCatmullRomInput;
        Slider alphaCatmullRomSlider;
        float alphaCatmullRomValue = 0.5f;

        InputFieldRef tensionCatmullRomInput;
        Slider tensionCatmullRomSlider;
        float tensionCatmullRomValue = 0;

        // ~~~~~~~~ UI construction / callbacks ~~~~~~~~

        protected override void ConstructPanelContent()
        {   
            GameObject horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "MainOptions", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleWidth: 9999);

            ButtonRef startButton = UIFactory.CreateButton(horiGroup, "Start", "▶");
            UIFactory.SetLayoutElement(startButton.GameObject, minWidth: 50, minHeight: 25);
            startButton.ButtonText.fontSize = 20;
            startButton.OnClick += StartButton_OnClick;

            ButtonRef pauseContinueButton = UIFactory.CreateButton(horiGroup, "Pause/Continue", "❚❚/▶");
            UIFactory.SetLayoutElement(pauseContinueButton.GameObject, minWidth: 50, minHeight: 25);
            pauseContinueButton.ButtonText.fontSize = 20;
            pauseContinueButton.OnClick += TogglePause_OnClick;

            ButtonRef stopButton = UIFactory.CreateButton(horiGroup, "Stop", "■");
            UIFactory.SetLayoutElement(stopButton.GameObject, minWidth: 50, minHeight: 25);
            stopButton.ButtonText.fontSize = 20;
            stopButton.OnClick += Stop_OnClick;

            ButtonRef AddNode = UIFactory.CreateButton(horiGroup, "AddCamNode", "+");
            UIFactory.SetLayoutElement(AddNode.GameObject, minWidth: 50, minHeight: 25);
            AddNode.OnClick += AddNode_OnClick;

            ButtonRef DeletePath = UIFactory.CreateButton(horiGroup, "DeletePath", "Delete Path");
            UIFactory.SetLayoutElement(DeletePath.GameObject, minWidth: 150, minHeight: 25);
            DeletePath.OnClick += () => {controlPoints.Clear(); UpdateListNodes(); ToggleVisualizePath(false);};

            Toggle closedLoopToggle = new Toggle();
            GameObject toggleClosedLoopObj = UIFactory.CreateToggle(horiGroup, "Close path in a loop", out closedLoopToggle, out Text toggleClosedLoopText);
            UIFactory.SetLayoutElement(toggleClosedLoopObj, minHeight: 25, flexibleWidth: 9999);
            closedLoopToggle.isOn = false;
            closedLoopToggle.onValueChanged.AddListener((isClosedLoop) => {closedLoop = isClosedLoop; MaybeRedrawPath(); EventSystemHelper.SetSelectedGameObject(null);});
            toggleClosedLoopText.text = "Close path in a loop";

            // CatmullRom alpha value
            GameObject catmullRomVariablesGroup = AddInputField("alphaCatmullRom", "Alpha:", "0.5", out alphaCatmullRomInput, AlphaCatmullRom_OnEndEdit, 100, false);
            alphaCatmullRomInput.Text = alphaCatmullRomValue.ToString();

            GameObject alphaCatmullRomObj = UIFactory.CreateSlider(catmullRomVariablesGroup, "Alpha CatmullRom Slider", out alphaCatmullRomSlider);
            UIFactory.SetLayoutElement(alphaCatmullRomObj, minHeight: 25, minWidth: 50, flexibleWidth: 0);
            alphaCatmullRomSlider.m_FillImage.color = Color.clear;
            alphaCatmullRomSlider.minValue = 0;
            alphaCatmullRomSlider.maxValue = 1;
            alphaCatmullRomSlider.value = alphaCatmullRomValue;
            alphaCatmullRomSlider.onValueChanged.AddListener((newAlpha) => {
                alphaCatmullRomValue = newAlpha;
                alphaCatmullRomInput.Text = alphaCatmullRomValue.ToString();

                MaybeRedrawPath();
            });

            // CatmullRom tension value
            AddInputField("tensionCatmullRomO", "Tension:", "0", out tensionCatmullRomInput, TensionCatmullRom_OnEndEdit, 100, false, catmullRomVariablesGroup);
            tensionCatmullRomInput.Text = tensionCatmullRomValue.ToString();

            GameObject tensionCatmullRomObj = UIFactory.CreateSlider(catmullRomVariablesGroup, "Tension CatmullRom Slider", out tensionCatmullRomSlider);
            UIFactory.SetLayoutElement(tensionCatmullRomObj, minHeight: 25, minWidth: 50, flexibleWidth: 0);
            tensionCatmullRomSlider.m_FillImage.color = Color.clear;
            tensionCatmullRomSlider.minValue = 0;
            tensionCatmullRomSlider.maxValue = 1;
            tensionCatmullRomSlider.value = tensionCatmullRomValue;
            tensionCatmullRomSlider.onValueChanged.AddListener((newTension) => {
                tensionCatmullRomValue = newTension;
                tensionCatmullRomInput.Text = tensionCatmullRomValue.ToString();

                MaybeRedrawPath();
            });

            InputFieldRef TimeInput = null;
            GameObject secondRow = AddInputField("Time", "Path time (in seconds at 60fps):", $"Default: {time}", out TimeInput, Time_OnEndEdit, 50, false);
            TimeInput.Text = time.ToString();

            GameObject visualizePathObj = UIFactory.CreateToggle(secondRow, "Visualize Path", out visualizePathToggle, out Text visualizePathText);
            UIFactory.SetLayoutElement(visualizePathObj, minHeight: 25, flexibleWidth: 9999);
            visualizePathToggle.isOn = false;
            visualizePathToggle.onValueChanged.AddListener(ToggleVisualizePath);
            visualizePathText.text = "Visualize path";

            GameObject unpauseOnPlayObj = UIFactory.CreateToggle(secondRow, "Unpause on play", out Toggle unpauseOnPlayToggle, out Text unpauseOnPlayText);
            UIFactory.SetLayoutElement(unpauseOnPlayObj, minHeight: 25, flexibleWidth: 9999);
            unpauseOnPlayToggle.isOn = false;
            unpauseOnPlayToggle.onValueChanged.AddListener((value) => unpauseOnPlay = value);
            unpauseOnPlayText.text = "Unpause on play";

            GameObject waitBeforePlayObj = UIFactory.CreateToggle(secondRow, "Wait before play", out Toggle waitBeforePlayToggle, out Text waitBeforePlayText);
            UIFactory.SetLayoutElement(waitBeforePlayObj, minHeight: 25, flexibleWidth: 9999);
            waitBeforePlayToggle.isOn = false;
            waitBeforePlayToggle.onValueChanged.AddListener((value) => waitBeforePlay = value);
            waitBeforePlayText.text = "Wait 3 seconds before start";
        }

        void AlphaCatmullRom_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                alphaCatmullRomInput.Text = alphaCatmullRomValue.ToString();
                return;
            }

            alphaCatmullRomValue = parsed;
            alphaCatmullRomSlider.value = alphaCatmullRomValue;

            MaybeRedrawPath();
        }

        void TensionCatmullRom_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                tensionCatmullRomInput.Text = tensionCatmullRomValue.ToString();
                return;
            }

            tensionCatmullRomValue = parsed;
            tensionCatmullRomSlider.value = tensionCatmullRomValue;

            MaybeRedrawPath();
        }

        private void UpdateListNodes(){
            //Refresh list
            foreach (var comp in UINodes){
                UnityEngine.Object.Destroy(comp);
                //UIElements.Remove(comp);
            }

            for(int i = 0; i < controlPoints.Count; i++) {
                CatmullRom.CatmullRomPoint point = controlPoints[i];
                DrawNodeOptions(point, i);
            }
        }

        private void ToggleVisualizePath(bool enable){
            // Had to include this check because the pathVisualizer was null for some reason
            if (pathVisualizer == null) pathVisualizer = new GameObject("PathVisualizer");
            if (enable){
                if (controlPoints.Count > 2){
                    UpdateCatmullRomMoverData();
                    
                    List<CatmullRom.CatmullRomPoint> lookaheadPoints = GetCameraPathsManager().GetLookaheadPoints();
                    int skip_points = 5; // How many points do we have to skip before drawing another arrow (otherwise they look very cluttered)
                    int n = 0;
                    for (int i=0; i < lookaheadPoints.Count; i++){
                        // We also want to draw an arrow on the last point in case its skipped.
                        if (n < skip_points && i != lookaheadPoints.Count - 1){ 
                            n++;
                            continue;
                        }

                        GameObject arrow = ArrowGenerator.CreateArrow(lookaheadPoints[i].position, lookaheadPoints[i].rotation);
                        arrow.transform.SetParent(pathVisualizer.transform, true);
                        n = 0;
                    }
                }

            }
            else {
                foreach (Transform transform in pathVisualizer.transform)
                {
                    UnityEngine.Object.Destroy(transform.gameObject);
                }
            }
        }

        private void MaybeRedrawPath(){
            if (visualizePathToggle.isOn){
                ToggleVisualizePath(false);
                ToggleVisualizePath(true);
            }
        }

        private void DrawNodeOptions(CatmullRom.CatmullRomPoint point, int index){
            GameObject horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "LightOptions", true, false, true, false, 4,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleWidth: 9999);
            UINodes.Add(horiGroup);

            //Move to Camera
            ButtonRef moveToCameraButton = UIFactory.CreateButton(horiGroup, "Copy Camera pos and rot", "Copy Camera pos and rot");
            UIFactory.SetLayoutElement(moveToCameraButton.GameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            moveToCameraButton.OnClick += () => {
                point.position = followObject != null ? FreeCamPanel.ourCamera.transform.localPosition : FreeCamPanel.ourCamera.transform.position;
                point.rotation = followObject != null ? FreeCamPanel.ourCamera.transform.localRotation : FreeCamPanel.ourCamera.transform.rotation;
                controlPoints[index] = point;

                MaybeRedrawPath();

                EventSystemHelper.SetSelectedGameObject(null);
            };

            ButtonRef copyFovButton = UIFactory.CreateButton(horiGroup, "Copy Camera FoV", "Copy Camera FoV");
            UIFactory.SetLayoutElement(copyFovButton.GameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            copyFovButton.OnClick += () => {point.fov = FreeCamPanel.ourCamera.fieldOfView; controlPoints[index] = point; EventSystemHelper.SetSelectedGameObject(null);};

            ButtonRef moveToPointButton = UIFactory.CreateButton(horiGroup, "Move Cam to Node", "Move Cam to Node");
            UIFactory.SetLayoutElement(moveToPointButton.GameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            moveToPointButton.OnClick += () => {
                if (followObject != null){
                    FreeCamPanel.ourCamera.transform.localPosition = point.position;
                    FreeCamPanel.ourCamera.transform.localRotation = point.rotation;
                } else {
                    FreeCamPanel.ourCamera.transform.position = point.position;
                    FreeCamPanel.ourCamera.transform.rotation = point.rotation;
                }
                FreeCamPanel.ourCamera.fieldOfView = point.fov;

                EventSystemHelper.SetSelectedGameObject(null);
            };

            //Add node next

            ButtonRef destroyButton = UIFactory.CreateButton(horiGroup, "Delete", "Delete");
            UIFactory.SetLayoutElement(destroyButton.GameObject, minWidth: 80, minHeight: 25, flexibleWidth: 9999);
            destroyButton.OnClick += () => {controlPoints.Remove(point); UpdateListNodes(); MaybeRedrawPath();};

            //ShowPos and rot?
        }

        GameObject AddInputField(string name, string labelText, string placeHolder, out InputFieldRef inputField, Action<string> onInputEndEdit, int inputMinWidth = 50, bool shouldDeleteOnUpdate = true, GameObject existingRow = null)
        {
            GameObject row = existingRow != null ? existingRow : UIFactory.CreateHorizontalGroup(ContentRoot, "Editor Field",
            false, false, true, true, 5, default, new Color(1, 1, 1, 0));
            if(shouldDeleteOnUpdate)
                UINodes.Add(row); //To delete it when we update the node list

            Text posLabel = UIFactory.CreateLabel(row, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(posLabel.gameObject, minWidth: 40, minHeight: 25);

            inputField = UIFactory.CreateInputField(row, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, minWidth: inputMinWidth, minHeight: 25);
            inputField.Component.GetOnEndEdit().AddListener(onInputEndEdit);

            return row;
        }

        void StartButton_OnClick()
        {
            if (waitBeforePlay) {
                if (startTimer.Enabled){
                    startTimer.Stop();
                }

                startTimer.Enabled = true;
            } else {
                StartPath();
            }

            EventSystemHelper.SetSelectedGameObject(null);
        }

        void StartPath(){
            if (GetCameraPathsManager()){
                UpdateCatmullRomMoverData();
                GetCameraPathsManager().StartPath();
                UIManager.ShowMenu = false;
                pathVisualizer.SetActive(false);
            }

            if (unpauseOnPlay && UIManager.GetTimeScaleWidget().IsPaused()){
                    UIManager.GetTimeScaleWidget().PauseToggle();
            }
        }

        void TogglePause_OnClick(){
            if(GetCameraPathsManager()){
                GetCameraPathsManager().TogglePause();
                pathVisualizer.SetActive(!GetCameraPathsManager().IsPaused());
            }

            EventSystemHelper.SetSelectedGameObject(null);
        }

        void Stop_OnClick(){
            if (GetCameraPathsManager()){
                GetCameraPathsManager().Stop();
                pathVisualizer.SetActive(true);
            }

            EventSystemHelper.SetSelectedGameObject(null);
        }

        void AddNode_OnClick(){
            EventSystemHelper.SetSelectedGameObject(null);

            Camera freeCam = FreeCamPanel.ourCamera;
            CatmullRom.CatmullRomPoint point = new CatmullRom.CatmullRomPoint(
                followObject != null ? freeCam.transform.localPosition : freeCam.transform.position,
                followObject != null ? freeCam.transform.localRotation : freeCam.transform.rotation,
                freeCam.fieldOfView
            );

            controlPoints.Add(point);
            UpdateListNodes();
            MaybeRedrawPath();
        }

        void Time_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out int parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                return;
            }

            time = parsed;
            if(GetCameraPathsManager()){
                GetCameraPathsManager().setTime(time);
            }
        }

        private CatmullRom.CatmullRomMover GetCameraPathsManager(){
            return FreeCamPanel.cameraPathMover;
        }

        public void UpdatedFollowObject(GameObject obj){
            // Had to include this check because the pathVisualizer was null for some reason
            if (pathVisualizer == null) pathVisualizer = new GameObject("PathVisualizer");
            if (followObject != null){
                TranslatePointsToGlobal();
                pathVisualizer.transform.SetParent(null, true);
            }
            followObject = obj;
            if (obj != null){
                TranslatePointsToLocal();
                pathVisualizer.transform.SetParent(obj.transform, true);
            }
            UpdateListNodes();
        }

        void TranslatePointsToGlobal() {
            List<CatmullRom.CatmullRomPoint> newControlPoints = new List<CatmullRom.CatmullRomPoint>();
            foreach(CatmullRom.CatmullRomPoint point in controlPoints){
                Vector3 newPos = followObject.transform.TransformPoint(point.position);
                Quaternion newRot = followObject.transform.rotation * point.rotation;
                CatmullRom.CatmullRomPoint newPoint = new CatmullRom.CatmullRomPoint(newPos, newRot, point.fov);

                newControlPoints.Add(newPoint);
            }

            controlPoints = newControlPoints;
        }

        void TranslatePointsToLocal() {
            List<CatmullRom.CatmullRomPoint> newControlPoints = new List<CatmullRom.CatmullRomPoint>();
            foreach(CatmullRom.CatmullRomPoint point in controlPoints){
                Vector3 newPos = followObject.transform.InverseTransformPoint(point.position);
                Quaternion newRot = Quaternion.Inverse(followObject.transform.rotation) * point.rotation;
                CatmullRom.CatmullRomPoint newPoint = new CatmullRom.CatmullRomPoint(newPos, newRot, point.fov);

                newControlPoints.Add(newPoint);
            }

            controlPoints = newControlPoints;
        }

        void UpdateCatmullRomMoverData(){
            GetCameraPathsManager().setClosedLoop(closedLoop);
            GetCameraPathsManager().setLocalPoints(followObject != null);
            GetCameraPathsManager().setSplinePoints(controlPoints.ToArray());
            GetCameraPathsManager().setTime(time);
            GetCameraPathsManager().setCatmullRomVariables(alphaCatmullRomValue, tensionCatmullRomValue);

            GetCameraPathsManager().CalculateLookahead();
        }
    }
}
