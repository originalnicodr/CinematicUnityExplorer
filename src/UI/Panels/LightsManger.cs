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
    public class LightsManager : UEPanel, ICellPoolDataSource<LightCell>
    {
        public LightsManager(UIBase owner) : base(owner)
        {
        }

        public override string Name => "Lights Manager";
        public override UIManager.Panels PanelType => UIManager.Panels.LightsManager;
        public override int MinWidth => 650;
        public override int MinHeight => 200;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;

        static ButtonRef createSpotLightButton;
        static ButtonRef createPointLightButton;
        List<UnityEngine.Light> vanillaGameLights = new List<UnityEngine.Light>();

        public List<GameObject> CreatedLights = new List<GameObject>();
        //Declaring a counter instead of just using the length of the createdLight list in case we delete lights, so we don't end up with two lists with the same name.
        public int lightCounter = 0;
        float defaultIntensity = 10;

        public ScrollPool<LightCell> lightsScrollPool;
        public int ItemCount => CreatedLights.Count;
        private static bool DoneScrollPoolInit;

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            if (active && !DoneScrollPoolInit)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.Rect);
                lightsScrollPool.Initialize(this);
                DoneScrollPoolInit = true;
            }

            lightsScrollPool.Refresh(true, false);
        }

        public void OnCellBorrowed(LightCell cell) { }

        public void SetCell(LightCell cell, int index){
            if (index >= CreatedLights.Count)
            {
                cell.Disable();
                return;
            }

            GameObject light = CreatedLights[index];

            // Check if the light was deleted because we switched scenes, and delete it from the list if that's the case
            try {
                string check = light.name;
            }
            catch {
                ExplorerCore.LogWarning("Light was deleted by the game!");
                CreatedLights.RemoveAt(index);
                lightsScrollPool.Refresh(true, false);
                return;
            }

            cell.light = light;
            cell.label.text = light.name;
        }

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

            AddInputField(horiGroup, "Default Itensity", "Default itensity:", $"{10}", DefaultIntensity_OnEndEdit);

            //Turn off vanilla lights
            Toggle vanillaLightsToggle = new Toggle();
            GameObject toggleObj = UIFactory.CreateToggle(horiGroup, "Toggle Game Lights", out vanillaLightsToggle, out Text toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);
            vanillaLightsToggle.onValueChanged.AddListener(ToggleGameLights);
            vanillaLightsToggle.isOn = true;
            toggleText.text = "Vanilla Game Lights";

            lightsScrollPool = UIFactory.CreateScrollPool<LightCell>(ContentRoot, "NodeList", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.03f, 0.03f, 0.03f));
            UIFactory.SetLayoutElement(scrollObj, flexibleWidth: 9999, flexibleHeight: 9999);
        }

        private void ToggleGameLights(bool areLightsActive){
            //Update the list of game lights
            if (!areLightsActive){
                var gameLights = RuntimeHelper.FindObjectsOfTypeAll<UnityEngine.Light>().Where(l => l.enabled);

                //ExplorerCore.LogWarning($"Found ligths: {gameLights.Length}");

                foreach(UnityEngine.Light light in gameLights)
                {   
                    //We want to avoid grabbing our own lights
                    if(!light.name.Contains("CUE - Light")){
                        vanillaGameLights.Add(light);
                        light.enabled = false;
                    }
                }
            }
            else{
                foreach(UnityEngine.Light light in vanillaGameLights)
                {   
                    // In case the light was destroyed
                    if (light != null) light.enabled = true;
                }
                vanillaGameLights.Clear();
            }
        }

        private void CreateLight(LightType requestedType){
            GameObject obj = new($"CUE - Light {lightCounter}");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            Light lightComponent = obj.AddComponent<UnityEngine.Light>();
            lightComponent.type = requestedType;
            lightComponent.shadows = LightShadows.Soft;
            lightComponent.shadowBias = 0;
            lightComponent.intensity = defaultIntensity;

            PropertyInfo shadowResolution = typeof(Light).GetProperty("shadowCustomResolution");
            shadowResolution.SetValue(lightComponent, 5000, null);

            switch(requestedType){
                case LightType.Spot:
                    lightComponent.range = 10;
                    GameObject arrow = ArrowGenerator.CreateArrow(Vector3.zero, Quaternion.identity, lightComponent.color);
                    arrow.SetActive(false);
                    arrow.transform.SetParent(obj.transform, true);
                    break;
                case LightType.Point:
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

                    // Assign material and color to sphere
                    Renderer renderer = sphere.GetComponent<Renderer>();
                    renderer.material = new Material(Shader.Find("Sprites/Default"));
                    renderer.material.color = lightComponent.color;

                    sphere.SetActive(false);
                    sphere.transform.SetParent(obj.transform, true);
                    break;
            }

            LightCell.CopyFreeCamTransform(obj);

            CreatedLights.Add(obj);
            lightsScrollPool.Refresh(true, false);

            lightCounter++;
        }

        GameObject AddInputField(GameObject parent, string name, string labelText, string placeHolder, Action<string> onInputEndEdit)
        {
            Text posLabel = UIFactory.CreateLabel(parent, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(posLabel.gameObject, minWidth: 75, minHeight: 25);

            InputFieldRef inputField = UIFactory.CreateInputField(parent, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, minWidth: 50, minHeight: 25);
            inputField.Component.GetOnEndEdit().AddListener(onInputEndEdit);

            return parent;
        }

        void DefaultIntensity_OnEndEdit(string input)
        {
            if (!ParseUtility.TryParse(input, out int parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                return;
            }

            defaultIntensity = parsed;
        }
    }
}
