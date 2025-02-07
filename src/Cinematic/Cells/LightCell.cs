using UnityEngine;
using UnityExplorer.Config;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.UI.Panels
{
    public class LightCell : ICell
    {
        // ICell
        public float DefaultHeight => 25f;
        public GameObject UIRoot { get; set; }
        public RectTransform Rect { get; set; }

        public bool Enabled => UIRoot.activeSelf;
        public void Enable() => UIRoot.SetActive(true);
        public void Disable() => UIRoot.SetActive(false);

        //public int index;
        public GameObject light;
        public Text label;

        public virtual GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateUIObject("LightCell", parent, new Vector2(25, 25));
            Rect = UIRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 3);
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, minWidth: 50, flexibleWidth: 9999);

            //Active toggle
            Toggle toggleLight;
            GameObject toggleObj = UIFactory.CreateToggle(UIRoot, "ActivateLightToggle", out toggleLight, out label);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);
            toggleLight.onValueChanged.AddListener(value => { light.SetActive(value); });
            toggleLight.isOn = true;

            //Toggle visualizer
            ButtonRef toggleVisualizerButton = UIFactory.CreateButton(UIRoot, "ToggleVisualizer", "Toggle Visualizer");
            UIFactory.SetLayoutElement(toggleVisualizerButton.GameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            toggleVisualizerButton.OnClick += ToggleVisualizer;

            //Inspect GameObject
            ButtonRef inspectButton = UIFactory.CreateButton(UIRoot, "InspectButton", "Config");
            UIFactory.SetLayoutElement(inspectButton.GameObject, minWidth: 50, minHeight: 25, flexibleWidth: 9999);
            inspectButton.OnClick += () => InspectorManager.InspectWithFilters(light.GetComponent<Light>(), "Light",
                UnityExplorer.Inspectors.MemberFilter.Property
            );

            //Move to Camera
            ButtonRef moveToCameraButton = UIFactory.CreateButton(UIRoot, "MoveToCamera", "Move to Camera");
            UIFactory.SetLayoutElement(moveToCameraButton.GameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            moveToCameraButton.OnClick += () => CopyFreeCamTransform(light);

            //Attatch to Camera
            //TODO

            //Copy
            ButtonRef copyButton = UIFactory.CreateButton(UIRoot, "Copy", "Copy");
            UIFactory.SetLayoutElement(copyButton.GameObject, minWidth: 40, minHeight: 25, flexibleWidth: 9999);
            copyButton.OnClick += CopyLight;

            //Destroy Light
            ButtonRef destroyButton = UIFactory.CreateButton(UIRoot, "DestroyButton", "Destroy");
            UIFactory.SetLayoutElement(destroyButton.GameObject, minWidth: 50, minHeight: 25, flexibleWidth: 9999);
            destroyButton.OnClick += DestroyLight;

            return UIRoot;
        }

        private void ToggleVisualizer(){
            GameObject visualizer = light.transform.GetChild(0).gameObject;
            float arrowSize = ConfigManager.Arrow_Size.Value;
            Vector3 arrowSizeVec = new Vector3(Math.Max(arrowSize, 0.1f), Math.Max(arrowSize, 0.1f), Math.Max(arrowSize, 0.1f));
            light.transform.GetChild(0).localScale = arrowSizeVec;
            visualizer.SetActive(!visualizer.activeSelf);
        }

        private void CopyLight(){
            GameObject newLight = UnityEngine.Object.Instantiate(light);
            CopyFreeCamTransform(newLight);
            newLight.name = $"CUE - Light {GetLightsManagerPanel().lightCounter}";

            GetLightsManagerPanel().CreatedLights.Add(newLight);
            GetLightsManagerPanel().lightsScrollPool.Refresh(true, false);

            GetLightsManagerPanel().lightCounter++;
        }

        private void DestroyLight(){
            GetLightsManagerPanel().CreatedLights.Remove(light);
            UnityEngine.Object.Destroy(light);
            GetLightsManagerPanel().lightsScrollPool.Refresh(true, false);
        }

        public static void CopyFreeCamTransform(GameObject obj){
            Camera freeCam = FreeCamPanel.GetFreecam();
            
            if (freeCam != null) {
                obj.transform.position = freeCam.transform.position;
                obj.transform.rotation = freeCam.transform.rotation;

                return;
            }
        }

        public UnityExplorer.UI.Panels.LightsManager GetLightsManagerPanel(){
            return UIManager.GetPanel<UnityExplorer.UI.Panels.LightsManager>(UIManager.Panels.LightsManager);
        }
    }
}
