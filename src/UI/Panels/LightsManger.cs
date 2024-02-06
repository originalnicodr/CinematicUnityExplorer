using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using UniverseLib.UI.Widgets.ScrollView;
#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif
#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif

using UnityEngine.Rendering;
using UnityEngine;

namespace UnityExplorer.UI.Panels
{
    internal class LightsManager : UEPanel
    {
        public LightsManager(UIBase owner) : base(owner)
        {
        }

        public override string Name => "Lights Manager";
        public override UIManager.Panels PanelType => UIManager.Panels.LightsManager;
        public override int MinWidth => 600;
        public override int MinHeight => 500;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;

        static ButtonRef createSpotLightButton;
        static ButtonRef createPointLightButton;
        List<UnityEngine.Light> vanillaGameLights = new List<UnityEngine.Light>();

        List<GameObject> CreatedLights = new List<GameObject>();
        List<GameObject> UIElements = new List<GameObject>();
        //Declaring a counter instead of just using the length of the createdLight list in case we delete lights, so we don't end up with two lists with the same name.
        int lightCounter = 0;

        // ~~~~~~~~ UI construction / callbacks ~~~~~~~~

        protected override void ConstructPanelContent()
        {
            GameObject horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "MainOptions", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleWidth: 9999);
            //UIElements.Add(horiGroup);

            //Create SpotLight
            createSpotLightButton = UIFactory.CreateButton(horiGroup, "ToggleButton", "Create SpotLight");
            UIFactory.SetLayoutElement(createSpotLightButton.GameObject, minWidth: 150, minHeight: 25);
            createSpotLightButton.OnClick += () => CreateLight(LightType.Spot);

            //Create PointLight
            createPointLightButton = UIFactory.CreateButton(horiGroup, "ToggleButton", "Create PointLight");
            UIFactory.SetLayoutElement(createPointLightButton.GameObject, minWidth: 150, minHeight: 25);
            createPointLightButton.OnClick += () => CreateLight(LightType.Point);

            //Turn off vanilla lights
            Toggle vanillaLightsToggle = new Toggle();
            GameObject toggleObj = UIFactory.CreateToggle(horiGroup, "Toggle Game Lights", out vanillaLightsToggle, out Text toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);
            vanillaLightsToggle.onValueChanged.AddListener(ToggleGameLights);
            vanillaLightsToggle.isOn = true;
            toggleText.text = "Toggle Game Lights";

            ListCreatedLights();
        }

        private void ToggleGameLights(bool areLightsActive){
            //Update the list of game lights
            if (!areLightsActive){
                var gameLights = RuntimeHelper.FindObjectsOfTypeAll<UnityEngine.Light>();

                //ExplorerCore.LogWarning($"Found ligths: {gameLights.Length}");

                foreach(UnityEngine.Light light in gameLights)
                {   
                    //We want to avoid grabbing our own lights
                    if(!light.name.Contains("UE - Light")){
                        vanillaGameLights.Add(light);
                        light.enabled = false;
                    }
                }
            }
            else{
                foreach(UnityEngine.Light light in vanillaGameLights)
                {   
                    light.enabled = true;
                }
                vanillaGameLights.Clear();
            }
        }

        private void ListCreatedLights(){
            //Refresh list
            foreach (var comp in UIElements){
                UnityEngine.Object.Destroy(comp);
                //UIElements.Remove(comp);
            }

            foreach (var light in CreatedLights) {
                DrawOptionsLights(light);
            }
        }

        private void DrawOptionsLights(GameObject light){
            GameObject horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "LightOptions", true, false, true, false, 4,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleWidth: 9999);
            UIElements.Add(horiGroup);

            //Active toggle
            Toggle useGameCameraToggle = new Toggle();
            GameObject toggleObj = UIFactory.CreateToggle(horiGroup, "UseGameCameraToggle", out useGameCameraToggle, out Text toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);
            useGameCameraToggle.onValueChanged.AddListener(value => { light.SetActive(value); });
            useGameCameraToggle.isOn = true;
            toggleText.text = light.name;

            //Toggle visualizer
            ButtonRef toggleVisualizerButton = UIFactory.CreateButton(horiGroup, "ToggleVisualizer", "Toggle Visualizer");
            UIFactory.SetLayoutElement(toggleVisualizerButton.GameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            toggleVisualizerButton.OnClick += () => {ToggleVisualizer(light);};

            //Inspect GameObject
            ButtonRef inspectButton = UIFactory.CreateButton(horiGroup, "InspectButton", "Config");
            UIFactory.SetLayoutElement(inspectButton.GameObject, minWidth: 80, minHeight: 25, flexibleWidth: 9999);
            inspectButton.OnClick += () => InspectorManager.InspectWithFilters(light.GetComponent<Light>(), "Light",
                UnityExplorer.Inspectors.MemberFilter.Property |
                UnityExplorer.Inspectors.MemberFilter.Field
            );

            //Move to Camera
            ButtonRef moveToCameraButton = UIFactory.CreateButton(horiGroup, "MoveToCamera", "Move to Camera");
            UIFactory.SetLayoutElement(moveToCameraButton.GameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            moveToCameraButton.OnClick += () => {CopyFreeCamTransform(light);};

            //Attatch to Camera
            //TODO

            //Copy
            ButtonRef copyButton = UIFactory.CreateButton(horiGroup, "Copy", "Copy");
            UIFactory.SetLayoutElement(copyButton.GameObject, minWidth: 50, minHeight: 25, flexibleWidth: 9999);
            copyButton.OnClick += () => {CopyLight(light);};

            //Destroy Light
            ButtonRef destroyButton = UIFactory.CreateButton(horiGroup, "DestroyButton", "Destroy");
            UIFactory.SetLayoutElement(destroyButton.GameObject, minWidth: 80, minHeight: 25, flexibleWidth: 9999);
            destroyButton.OnClick += () => {DestroyLight(light);};
        }

        private void CreateLight(LightType requestedType){
            GameObject obj = new($"UE - Light {lightCounter}");
            //DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.AddComponent<UnityEngine.Light>();

            obj.GetComponent<Light>().type = requestedType;

            switch(requestedType){
                case LightType.Spot:
                    obj.GetComponent<Light>().intensity = 200;
                    obj.GetComponent<Light>().range = 1000;
                    GameObject arrow = ArrowGenerator.CreateArrow(Vector3.zero, Quaternion.identity);
                    arrow.SetActive(false);
                    arrow.transform.SetParent(obj.transform, true);
                    break;
                case LightType.Point:
                    obj.GetComponent<Light>().intensity = 10;
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.SetActive(false);
                    sphere.transform.SetParent(obj.transform, true);
                    break;
            }

            CopyFreeCamTransform(obj);

            CreatedLights.Add(obj);
            ListCreatedLights();

            lightCounter++;
        }

        private void ToggleVisualizer(GameObject light){
            GameObject visualizer = light.transform.GetChild(0).gameObject;
            visualizer.SetActive(!visualizer.activeSelf);
        }

        private void CopyLight(GameObject light){
            GameObject newLight = UnityEngine.Object.Instantiate(light);
            CopyFreeCamTransform(newLight);
            newLight.name = $"UE - Light {lightCounter}";

            CreatedLights.Add(newLight);
            ListCreatedLights();

            lightCounter++;
        }

        private void DestroyLight(GameObject light){
            CreatedLights.Remove(light);
            UnityEngine.Object.Destroy(light);
            ListCreatedLights();
        }

        private void CopyFreeCamTransform(GameObject obj){
            Camera freeCam = FreeCamPanel.ourCamera;
            
            if (freeCam != null) {
                obj.transform.position = freeCam.transform.position;
                obj.transform.rotation = freeCam.transform.rotation;

                return;
            }
        }
    }
}
